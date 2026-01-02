import envoy
import gleam/int
import gleam/result

pub const default_port = 5004

pub const default_poll_interval_ms = 10_000

pub const degraded_latency_threshold_ms = 100

pub const unhealthy_failure_threshold = 3

pub const history_depth = 10

pub fn get_poll_interval() -> Int {
  envoy.get("POLL_INTERVAL_MS")
  |> result.try(int.parse)
  |> result.unwrap(default_poll_interval_ms)
}

pub fn get_events_url() -> String {
  envoy.get("EVENTS_SERVICE_URL")
  |> result.unwrap("http://events:5005")
}
