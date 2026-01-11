import birl
import gleam/dict.{type Dict}
import gleam/erlang/process.{type Subject}
import gleam/int
import gleam/list
import gleam/option.{type Option, None, Some}
import health/checker.{type CheckerDeps}
import health/events.{type EventPublisher}
import health/types.{
  type AggregatedStatus, type HealthCheck, type Service, type ServiceStatus,
  type Status, type StatusConfig, AggregatedStatus, Degraded, Healthy,
  ServiceStatus, Unhealthy,
}
import logging

// ============================================================================
// Public Types
// ============================================================================

pub opaque type Poller {
  Poller(subject: Subject(Message))
}

/// Dependencies for the poller (injectable for testing)
pub type PollerDeps {
  PollerDeps(
    load_services: fn() -> List(Service),
    create_publisher: fn() -> EventPublisher,
    checker: CheckerDeps,
    status_config: StatusConfig,
    poll_interval_ms: Int,
  )
}

pub type Message {
  Poll
  GetStatus(reply_with: Subject(AggregatedStatus))
  GetServiceStatus(name: String, reply_with: Subject(Option(ServiceStatus)))
  Shutdown
}

// ============================================================================
// Internal State
// ============================================================================

type State {
  State(
    services: List(Service),
    statuses: Dict(String, ServiceStatus),
    publisher: EventPublisher,
    unhealthy_since: Dict(String, birl.Time),
    deps: PollerDeps,
  )
}

// ============================================================================
// Public API
// ============================================================================

/// Start poller with explicit dependencies
pub fn start_with_deps(deps: PollerDeps) -> Result(Poller, Nil) {
  let parent_subject: Subject(Subject(Message)) = process.new_subject()

  process.spawn(fn() {
    let message_subject = process.new_subject()
    process.send(parent_subject, message_subject)

    let publisher = deps.create_publisher()
    let services = deps.load_services()

    logging.log(
      logging.Info,
      "Loaded " <> int.to_string(list.length(services)) <> " services to monitor",
    )

    process.send(message_subject, Poll)

    let initial_state =
      State(
        services: services,
        statuses: dict.new(),
        publisher: publisher,
        unhealthy_since: dict.new(),
        deps: deps,
      )
    run_loop(message_subject, initial_state)
  })

  let assert Ok(message_subject) =
    process.receive(from: parent_subject, within: 5000)

  Ok(Poller(message_subject))
}

pub fn get_status(poller: Poller) -> Result(AggregatedStatus, Nil) {
  let Poller(subject) = poller
  let reply_subject = process.new_subject()
  process.send(subject, GetStatus(reply_subject))

  case process.receive(from: reply_subject, within: 5000) {
    Ok(result) -> Ok(result)
    Error(_) -> {
      logging.log(
        logging.Error,
        "get_status timed out waiting for poller response",
      )
      Error(Nil)
    }
  }
}

pub fn get_service_status(poller: Poller, name: String) -> Option(ServiceStatus) {
  let Poller(subject) = poller
  let reply_subject = process.new_subject()
  process.send(subject, GetServiceStatus(name, reply_subject))

  case process.receive(from: reply_subject, within: 5000) {
    Ok(result) -> result
    Error(_) -> {
      logging.log(
        logging.Error,
        "get_service_status timed out waiting for poller response",
      )
      None
    }
  }
}

// ============================================================================
// Actor Loop
// ============================================================================

fn run_loop(self: Subject(Message), state: State) -> Nil {
  let selector =
    process.new_selector()
    |> process.select(for: self)

  case process.selector_receive(from: selector, within: 60_000) {
    Ok(Poll) -> {
      let #(new_statuses, new_unhealthy_since) =
        poll_all_services(state)

      process.send_after(self, state.deps.poll_interval_ms, Poll)

      run_loop(
        self,
        State(
          ..state,
          statuses: new_statuses,
          unhealthy_since: new_unhealthy_since,
        ),
      )
    }

    Ok(GetStatus(reply_with)) -> {
      let status = build_aggregated_status(state.statuses)
      process.send(reply_with, status)
      run_loop(self, state)
    }

    Ok(GetServiceStatus(name, reply_with)) -> {
      let status = dict.get(state.statuses, name) |> option.from_result
      process.send(reply_with, status)
      run_loop(self, state)
    }

    Ok(Shutdown) -> {
      logging.log(logging.Info, "Poller shutting down")
      Nil
    }

    Error(Nil) -> {
      run_loop(self, state)
    }
  }
}

// ============================================================================
// Polling Logic
// ============================================================================

fn poll_all_services(
  state: State,
) -> #(Dict(String, ServiceStatus), Dict(String, birl.Time)) {
  list.fold(
    state.services,
    #(state.statuses, state.unhealthy_since),
    fn(acc, service) {
      let #(statuses, unhealthy_times) = acc
      poll_single_service(service, statuses, unhealthy_times, state)
    },
  )
}

fn poll_single_service(
  service: Service,
  statuses: Dict(String, ServiceStatus),
  unhealthy_times: Dict(String, birl.Time),
  state: State,
) -> #(Dict(String, ServiceStatus), Dict(String, birl.Time)) {
  // Check the service
  let check = checker.check(service, state.deps.checker)

  // Update history
  let history = get_history(statuses, service.name)
  let new_history =
    [check, ..history] |> list.take(state.deps.status_config.history_depth)

  // Calculate new status
  let new_status = types.calculate_status(new_history, state.deps.status_config)

  // Handle status transitions
  let previous_status = get_previous_status(statuses, service.name)
  let new_unhealthy_times =
    handle_status_transition(
      service.name,
      previous_status,
      new_status,
      new_history,
      check,
      unhealthy_times,
      state.publisher,
    )

  // Build service status
  let service_status =
    ServiceStatus(
      name: service.name,
      status: new_status,
      latency_ms: check.latency_ms,
      last_check: check.checked_at,
      error: check.error,
      history: new_history,
    )

  #(dict.insert(statuses, service.name, service_status), new_unhealthy_times)
}

fn get_history(
  statuses: Dict(String, ServiceStatus),
  name: String,
) -> List(HealthCheck) {
  case dict.get(statuses, name) {
    Ok(status) -> status.history
    Error(_) -> []
  }
}

fn get_previous_status(
  statuses: Dict(String, ServiceStatus),
  name: String,
) -> Option(Status) {
  case dict.get(statuses, name) {
    Ok(prev) -> Some(prev.status)
    Error(_) -> None
  }
}

// ============================================================================
// Status Transition Events
// ============================================================================

fn handle_status_transition(
  service_name: String,
  previous: Option(Status),
  current: Status,
  history: List(HealthCheck),
  check: HealthCheck,
  unhealthy_times: Dict(String, birl.Time),
  publisher: EventPublisher,
) -> Dict(String, birl.Time) {
  case previous, current {
    // Became unhealthy
    Some(Healthy), Unhealthy
    | Some(Degraded), Unhealthy
    | None, Unhealthy
    -> {
      logging.log(logging.Warning, "Service became unhealthy: " <> service_name)
      let _ =
        events.publish_service_unhealthy(
          publisher,
          service_name,
          "unhealthy",
          types.count_consecutive_failures(history),
          check.error,
        )
      dict.insert(unhealthy_times, service_name, birl.now())
    }

    // Recovered
    Some(Unhealthy), Healthy -> {
      logging.log(logging.Info, "Service recovered: " <> service_name)
      let downtime = calculate_downtime(unhealthy_times, service_name)
      let _ =
        events.publish_service_recovered(publisher, service_name, downtime)
      dict.delete(unhealthy_times, service_name)
    }

    // No transition of interest
    _, _ -> unhealthy_times
  }
}

fn calculate_downtime(
  unhealthy_times: Dict(String, birl.Time),
  service_name: String,
) -> Int {
  case dict.get(unhealthy_times, service_name) {
    Ok(since) -> {
      let now_unix = birl.to_unix(birl.now())
      let since_unix = birl.to_unix(since)
      now_unix - since_unix
    }
    Error(_) -> 0
  }
}

// ============================================================================
// Aggregation
// ============================================================================

fn build_aggregated_status(
  statuses: Dict(String, ServiceStatus),
) -> AggregatedStatus {
  let services = dict.values(statuses)
  let overall = types.aggregate_services(services)
  AggregatedStatus(overall: overall, checked_at: birl.now(), services: services)
}
