import birl
import gleam/io
import gleam/json
import gleam/option.{type Option, None, Some}
import gleam/string
import health/config

pub type EventPublisher {
  EventPublisher(events_url: String)
}

pub fn new() -> EventPublisher {
  let url = config.get_events_url()
  EventPublisher(events_url: url)
}

pub fn publish_service_unhealthy(
  publisher: EventPublisher,
  service_name: String,
  status: String,
  consecutive_failures: Int,
  last_error: Option(String),
) -> Result(Nil, String) {
  let event_id = generate_uuid()
  let now = birl.now()

  let data =
    json.object([
      #("service_name", json.string(service_name)),
      #("status", json.string(status)),
      #("consecutive_failures", json.int(consecutive_failures)),
      #(
        "last_error",
        case last_error {
          Some(e) -> json.string(e)
          None -> json.null()
        },
      ),
      #("detected_at", json.string(birl.to_iso8601(now))),
    ])

  let cloud_event =
    json.object([
      #("specversion", json.string("1.0")),
      #("id", json.string(event_id)),
      #("source", json.string("hellarch/health")),
      #("type", json.string("hellarch.infra.service.unhealthy.v1")),
      #("datacontenttype", json.string("application/json")),
      #("time", json.string(birl.to_iso8601(now))),
      #("data", data),
    ])

  publish_event(publisher, cloud_event)
}

pub fn publish_service_recovered(
  publisher: EventPublisher,
  service_name: String,
  downtime_seconds: Int,
) -> Result(Nil, String) {
  let event_id = generate_uuid()
  let now = birl.now()

  let data =
    json.object([
      #("service_name", json.string(service_name)),
      #("recovered_at", json.string(birl.to_iso8601(now))),
      #("downtime_seconds", json.int(downtime_seconds)),
    ])

  let cloud_event =
    json.object([
      #("specversion", json.string("1.0")),
      #("id", json.string(event_id)),
      #("source", json.string("hellarch/health")),
      #("type", json.string("hellarch.infra.service.recovered.v1")),
      #("datacontenttype", json.string("application/json")),
      #("time", json.string(birl.to_iso8601(now))),
      #("data", data),
    ])

  publish_event(publisher, cloud_event)
}

fn publish_event(
  publisher: EventPublisher,
  event: json.Json,
) -> Result(Nil, String) {
  let url = publisher.events_url <> "/api/events/publish"
  let body = json.to_string(event)

  case do_http_post(url, body, 5000) {
    Ok(status) if status >= 200 && status < 300 -> {
      io.println("Event published successfully")
      Ok(Nil)
    }
    Ok(status) -> {
      let msg = "Events API returned status " <> string.inspect(status)
      io.println(msg)
      Error(msg)
    }
    Error(reason) -> {
      io.println("Failed to publish event: " <> reason)
      Error(reason)
    }
  }
}

@external(erlang, "health_events_ffi", "http_post")
fn do_http_post(url: String, body: String, timeout_ms: Int) -> Result(Int, String)

@external(erlang, "health_events_ffi", "generate_uuid")
fn generate_uuid() -> String
