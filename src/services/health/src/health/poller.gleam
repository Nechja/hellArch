import birl
import gleam/dict.{type Dict}
import gleam/erlang/process.{type Subject}
import gleam/int
import gleam/io
import gleam/list
import gleam/option.{type Option}
import gleam/string
import health/config
import health/http_client
import health/registry
import health/types.{
  type AggregatedStatus, type HealthCheck, type ServiceStatus, type Status,
  AggregatedStatus, Degraded, HealthCheck, Healthy, HttpGet, ServiceStatus,
  TcpConnect, Unhealthy,
}

pub opaque type Poller {
  Poller(subject: Subject(Message))
}

pub type Message {
  Poll
  GetStatus(reply_with: Subject(AggregatedStatus))
  GetServiceStatus(name: String, reply_with: Subject(Option(ServiceStatus)))
  Shutdown
}

type State {
  State(statuses: Dict(String, ServiceStatus))
}

const check_timeout_ms = 5000

pub fn start() -> Result(Poller, Nil) {
  let subject = process.new_subject()

  // Start the actor loop in a new process
  process.start(linked: True, running: fn() {
    run_loop(subject, State(statuses: dict.new()))
  })

  // Send initial poll message
  process.send(subject, Poll)

  Ok(Poller(subject))
}

fn run_loop(self: Subject(Message), state: State) -> Nil {
  let selector =
    process.new_selector()
    |> process.selecting(self, fn(msg) { msg })

  case process.select(selector, 60_000) {
    Ok(Poll) -> {
      io.println("Polling services...")
      let services = registry.services()
      let new_statuses = poll_all_services(services, state.statuses)

      // Schedule next poll
      let poll_interval = config.get_poll_interval()
      process.send_after(self, poll_interval, Poll)

      run_loop(self, State(statuses: new_statuses))
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
      io.println("Poller shutting down")
      Nil
    }

    Error(Nil) -> {
      // Timeout - just continue
      run_loop(self, state)
    }
  }
}

fn poll_all_services(
  services: List(types.Service),
  current_statuses: Dict(String, ServiceStatus),
) -> Dict(String, ServiceStatus) {
  list.fold(services, current_statuses, fn(acc, service) {
    let check = check_service(service)
    let history = get_history(acc, service.name)
    let new_history = [check, ..history] |> list.take(config.history_depth)
    let status = calculate_status(new_history)

    let service_status =
      ServiceStatus(
        name: service.name,
        status: status,
        latency_ms: check.latency_ms,
        last_check: check.checked_at,
        error: check.error,
        history: new_history,
      )

    dict.insert(acc, service.name, service_status)
  })
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

fn check_service(service: types.Service) -> HealthCheck {
  let start = birl.monotonic_now()

  let result = case service.check_type {
    HttpGet(path) -> {
      let url = service.url <> path
      case http_client.get(url, check_timeout_ms) {
        http_client.HttpOk(status, _) if status >= 200 && status < 300 ->
          Ok(Nil)
        http_client.HttpOk(status, _) ->
          Error("HTTP " <> int.to_string(status))
        http_client.HttpError(reason) -> Error(reason)
      }
    }
    TcpConnect -> {
      let #(host, port) = parse_host_port(service.url)
      case http_client.check_tcp_port(host, port, check_timeout_ms) {
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

pub fn get_status(poller: Poller) -> AggregatedStatus {
  let Poller(subject) = poller
  let reply_subject = process.new_subject()
  process.send(subject, GetStatus(reply_subject))

  let selector =
    process.new_selector()
    |> process.selecting(reply_subject, fn(msg) { msg })

  let assert Ok(result) = process.select(selector, 5000)
  result
}

pub fn get_service_status(poller: Poller, name: String) -> Option(ServiceStatus) {
  let Poller(subject) = poller
  let reply_subject = process.new_subject()
  process.send(subject, GetServiceStatus(name, reply_subject))

  let selector =
    process.new_selector()
    |> process.selecting(reply_subject, fn(msg) { msg })

  let assert Ok(result) = process.select(selector, 5000)
  result
}
