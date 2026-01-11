import birl.{type Time}
import gleam/list
import gleam/option.{type Option}

pub type Status {
  Healthy
  Degraded
  Unhealthy
}

pub type CheckType {
  HttpGet(path: String)
  TcpConnect
}

pub type Service {
  Service(name: String, url: String, check_type: CheckType, critical: Bool)
}

pub type HealthCheck {
  HealthCheck(
    status: Status,
    latency_ms: Int,
    checked_at: Time,
    error: Option(String),
  )
}

pub type ServiceStatus {
  ServiceStatus(
    name: String,
    status: Status,
    latency_ms: Int,
    last_check: Time,
    error: Option(String),
    history: List(HealthCheck),
  )
}

pub type AggregatedStatus {
  AggregatedStatus(
    overall: Status,
    checked_at: Time,
    services: List(ServiceStatus),
  )
}

/// Configuration for status calculation thresholds
pub type StatusConfig {
  StatusConfig(
    unhealthy_failure_threshold: Int,
    degraded_latency_threshold_ms: Int,
    history_depth: Int,
  )
}

/// Default status calculation config
pub fn default_status_config() -> StatusConfig {
  StatusConfig(
    unhealthy_failure_threshold: 3,
    degraded_latency_threshold_ms: 100,
    history_depth: 10,
  )
}

pub fn status_to_string(status: Status) -> String {
  case status {
    Healthy -> "healthy"
    Degraded -> "degraded"
    Unhealthy -> "unhealthy"
  }
}

pub fn combine_status(a: Status, b: Status) -> Status {
  case a, b {
    Unhealthy, _ -> Unhealthy
    _, Unhealthy -> Unhealthy
    Degraded, _ -> Degraded
    _, Degraded -> Degraded
    Healthy, Healthy -> Healthy
  }
}

/// Calculate overall status from health check history
pub fn calculate_status(history: List(HealthCheck), config: StatusConfig) -> Status {
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

/// Count consecutive failures from the start of history
pub fn count_consecutive_failures(history: List(HealthCheck)) -> Int {
  history
  |> list.take_while(fn(check) { check.status == Unhealthy })
  |> list.length
}

/// Aggregate status across multiple services
pub fn aggregate_services(services: List(ServiceStatus)) -> Status {
  list.fold(services, Healthy, fn(acc, svc) { combine_status(acc, svc.status) })
}
