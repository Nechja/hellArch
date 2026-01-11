import birl
import gleam/dict.{type Dict}
import gleam/erlang/process.{type Subject}
import gleam/int
import gleam/list
import gleam/option.{type Option, None, Some}
import gleam/string
import health/config
import health/events.{type EventPublisher}
import health/http_client
import health/types.{
  type AggregatedStatus, type HealthCheck, type Service, type ServiceStatus,
  type Status, AggregatedStatus, Degraded, HealthCheck, Healthy, HttpGet,
  ServiceStatus, TcpConnect, Unhealthy,
}
import logging

pub opaque type Poller {
  Poller(subject: Subject(Message))
}

/// Dependencies that can be injected for testing
pub type PollerDeps {
  PollerDeps(
    load_services: fn() -> List(Service),
    create_publisher: fn() -> EventPublisher,
    http_get: fn(String, Int) -> http_client.HttpResult,
    tcp_check: fn(String, Int, Int) -> Bool,
  )
}

pub type Message {
  Poll
  GetStatus(reply_with: Subject(AggregatedStatus))
  GetServiceStatus(name: String, reply_with: Subject(Option(ServiceStatus)))
  Shutdown
}

type State {
  State(
    services: List(Service),
    statuses: Dict(String, ServiceStatus),
    publisher: EventPublisher,
    unhealthy_since: Dict(String, birl.Time),
    deps: PollerDeps,
  )
}

const check_timeout_ms = 5000

/// Create default production dependencies
pub fn default_deps() -> PollerDeps {
  PollerDeps(
    load_services: fn() {
      // Import here to avoid circular dependency issues
      // In production this calls registry.services()
      panic as "Must provide load_services dependency"
    },
    create_publisher: events.new,
    http_get: http_client.get,
    tcp_check: http_client.check_tcp_port,
  )
}

/// Start poller with explicit dependencies (for testing)
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

fn run_loop(self: Subject(Message), state: State) -> Nil {
  let selector =
    process.new_selector()
    |> process.select(for: self)

  case process.selector_receive(from: selector, within: 60_000) {
    Ok(Poll) -> {
      let #(new_statuses, new_unhealthy_since) =
        poll_all_services_with_events(
          state.services,
          state.statuses,
          state.unhealthy_since,
          state.publisher,
          state.deps,
        )

      let poll_interval = config.get_poll_interval()
      process.send_after(self, poll_interval, Poll)

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
      let status = aggregate_status(state.statuses)
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

fn poll_all_services_with_events(
  services: List(Service),
  current_statuses: Dict(String, ServiceStatus),
  unhealthy_since: Dict(String, birl.Time),
  publisher: EventPublisher,
  deps: PollerDeps,
) -> #(Dict(String, ServiceStatus), Dict(String, birl.Time)) {
  list.fold(services, #(current_statuses, unhealthy_since), fn(acc, service) {
    let #(statuses, unhealthy_times) = acc
    let check = check_service(service, deps)
    let history = get_history(statuses, service.name)
    let new_history = [check, ..history] |> list.take(config.history_depth)
    let new_status = calculate_status(new_history)

    let previous_status = case dict.get(statuses, service.name) {
      Ok(prev) -> Some(prev.status)
      Error(_) -> None
    }

    let new_unhealthy_times = case previous_status, new_status {
      Some(Healthy), Unhealthy | Some(Degraded), Unhealthy | None, Unhealthy -> {
        logging.log(
          logging.Warning,
          "Service became unhealthy: " <> service.name,
        )
        let _ =
          events.publish_service_unhealthy(
            publisher,
            service.name,
            "unhealthy",
            count_consecutive_failures(new_history),
            check.error,
          )
        dict.insert(unhealthy_times, service.name, birl.now())
      }

      Some(Unhealthy), Healthy -> {
        logging.log(logging.Info, "Service recovered: " <> service.name)
        let downtime = case dict.get(unhealthy_times, service.name) {
          Ok(since) -> {
            let now_unix = birl.to_unix(birl.now())
            let since_unix = birl.to_unix(since)
            now_unix - since_unix
          }
          Error(_) -> 0
        }
        let _ =
          events.publish_service_recovered(publisher, service.name, downtime)
        dict.delete(unhealthy_times, service.name)
      }

      _, _ -> unhealthy_times
    }

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
  })
}

fn count_consecutive_failures(history: List(HealthCheck)) -> Int {
  history
  |> list.take_while(fn(check) { check.status == Unhealthy })
  |> list.length
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

fn check_service(service: Service, deps: PollerDeps) -> HealthCheck {
  let start = birl.monotonic_now()

  let result = case service.check_type {
    HttpGet(path) -> {
      let url = service.url <> path
      case deps.http_get(url, check_timeout_ms) {
        http_client.HttpOk(status, _) if status >= 200 && status < 300 ->
          Ok(Nil)
        http_client.HttpOk(status, _) ->
          Error("HTTP " <> int.to_string(status))
        http_client.HttpError(reason) -> Error(reason)
      }
    }
    TcpConnect -> {
      let #(host, port) = parse_host_port(service.url)
      case deps.tcp_check(host, port, check_timeout_ms) {
        True -> Ok(Nil)
        False -> Error("Connection refused")
      }
    }
  }

  let end = birl.monotonic_now()
  let latency_ms = { end - start } / 1_000_000

  case result {
    Ok(_) ->
      HealthCheck(
        status: Healthy,
        latency_ms: latency_ms,
        checked_at: birl.now(),
        error: option.None,
      )
    Error(reason) ->
      HealthCheck(
        status: Unhealthy,
        latency_ms: latency_ms,
        checked_at: birl.now(),
        error: option.Some(reason),
      )
  }
}

fn parse_host_port(url: String) -> #(String, Int) {
  case string.split(url, ":") {
    [host, port_str] ->
      case int.parse(port_str) {
        Ok(port) -> #(host, port)
        Error(_) -> #(url, 80)
      }
    _ -> #(url, 80)
  }
}

fn calculate_status(history: List(HealthCheck)) -> Status {
  let recent_failures =
    history
    |> list.take(config.unhealthy_failure_threshold)
    |> list.filter(fn(check) { check.status == Unhealthy })
    |> list.length

  let has_slow_response =
    list.any(history, fn(check) {
      check.latency_ms > config.degraded_latency_threshold_ms
    })

  case recent_failures, has_slow_response {
    n, _ if n >= config.unhealthy_failure_threshold -> Unhealthy
    n, _ if n > 0 -> Degraded
    _, True -> Degraded
    _, _ -> Healthy
  }
}

fn aggregate_status(statuses: Dict(String, ServiceStatus)) -> AggregatedStatus {
  let services = dict.values(statuses)

  let overall =
    list.fold(services, Healthy, fn(acc, svc) {
      types.combine_status(acc, svc.status)
    })

  AggregatedStatus(overall: overall, checked_at: birl.now(), services: services)
}

pub fn get_status(poller: Poller) -> Result(AggregatedStatus, Nil) {
  let Poller(subject) = poller
  let reply_subject = process.new_subject()
  process.send(subject, GetStatus(reply_subject))

  case process.receive(from: reply_subject, within: 5000) {
    Ok(result) -> Ok(result)
    Error(_) -> {
      logging.log(logging.Error, "get_status timed out waiting for poller response")
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
