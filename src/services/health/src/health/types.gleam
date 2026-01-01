import birl.{type Time}
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
