import birl
import gleam/int
import gleam/option
import gleam/string
import health/http_client
import health/types.{
  type HealthCheck, type Service, HealthCheck, Healthy, HttpGet, TcpConnect,
  Unhealthy,
}

/// Dependencies for checking services (injectable for testing)
pub type CheckerDeps {
  CheckerDeps(
    http_get: fn(String, Int) -> http_client.HttpResult,
    tcp_check: fn(String, Int, Int) -> Bool,
  )
}

/// Create production checker dependencies
pub fn default_deps() -> CheckerDeps {
  CheckerDeps(http_get: http_client.get, tcp_check: http_client.check_tcp_port)
}

const check_timeout_ms = 5000

/// Check a single service's health
pub fn check(service: Service, deps: CheckerDeps) -> HealthCheck {
  let start = birl.monotonic_now()

  let result = case service.check_type {
    HttpGet(path) -> check_http(service.url, path, deps.http_get)
    TcpConnect -> check_tcp(service.url, deps.tcp_check)
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

fn check_http(
  base_url: String,
  path: String,
  http_get: fn(String, Int) -> http_client.HttpResult,
) -> Result(Nil, String) {
  let url = base_url <> path
  case http_get(url, check_timeout_ms) {
    http_client.HttpOk(status, _) if status >= 200 && status < 300 -> Ok(Nil)
    http_client.HttpOk(status, _) -> Error("HTTP " <> int.to_string(status))
    http_client.HttpError(reason) -> Error(reason)
  }
}

fn check_tcp(
  url: String,
  tcp_check: fn(String, Int, Int) -> Bool,
) -> Result(Nil, String) {
  let #(host, port) = parse_host_port(url)
  case tcp_check(host, port, check_timeout_ms) {
    True -> Ok(Nil)
    False -> Error("Connection refused")
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
