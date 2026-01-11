import birl
import gleam/bytes_tree
import gleam/http/request.{type Request}
import gleam/http/response.{type Response}
import gleam/json
import gleam/list
import gleam/option.{None, Some}
import health/config
import health/http_client
import health/poller.{type Poller}
import health/types.{
  type AggregatedStatus, type HealthCheck, type ServiceStatus,
}
import mist.{type Connection, type ResponseData}

pub fn handler(
  poller: Poller,
) -> fn(Request(Connection)) -> Response(ResponseData) {
  fn(req: Request(Connection)) -> Response(ResponseData) {
    case request.path_segments(req) {
      ["health"] -> health_response()
      ["status"] -> status_response(poller)
      ["status", name] -> service_status_response(poller, name)
      ["workers"] -> workers_response()
      _ -> not_found_response()
    }
  }
}

fn health_response() -> Response(ResponseData) {
  json.object([#("status", json.string("healthy"))])
  |> json.to_string
  |> json_response(200, _)
}

fn status_response(poller: Poller) -> Response(ResponseData) {
  case poller.get_status(poller) {
    Ok(status) -> {
      encode_aggregated_status(status)
      |> json_response(200, _)
    }
    Error(_) -> {
      json.object([#("error", json.string("Poller unavailable"))])
      |> json.to_string
      |> json_response(503, _)
    }
  }
}

fn service_status_response(
  poller: Poller,
  name: String,
) -> Response(ResponseData) {
  case poller.get_service_status(poller, name) {
    Some(status) -> {
      encode_service_status(status)
      |> json_response(200, _)
    }
    None -> not_found_response()
  }
}

fn workers_response() -> Response(ResponseData) {
  let events_url = config.get_events_url()
  let url = events_url <> "/workers/status"

  case http_client.get(url, 5000) {
    http_client.HttpOk(status, body) if status >= 200 && status < 300 -> {
      json_response(200, body)
    }
    http_client.HttpOk(status, _) -> {
      json.object([#("error", json.string("Events API error"))])
      |> json.to_string
      |> json_response(status, _)
    }
    http_client.HttpError(reason) -> {
      json.object([#("error", json.string(reason))])
      |> json.to_string
      |> json_response(503, _)
    }
  }
}

fn not_found_response() -> Response(ResponseData) {
  json.object([#("error", json.string("Not found"))])
  |> json.to_string
  |> json_response(404, _)
}

fn json_response(status: Int, body: String) -> Response(ResponseData) {
  response.new(status)
  |> response.set_header("content-type", "application/json")
  |> response.set_body(mist.Bytes(bytes_tree.from_string(body)))
}

fn encode_aggregated_status(status: AggregatedStatus) -> String {
  let services_pairs =
    list.map(status.services, fn(svc) {
      #(svc.name, encode_service_status_json(svc))
    })

  json.object([
    #("overall", json.string(types.status_to_string(status.overall))),
    #("checked_at", json.string(birl.to_iso8601(status.checked_at))),
    #("services", json.object(services_pairs)),
  ])
  |> json.to_string
}

fn encode_service_status(status: ServiceStatus) -> String {
  json.object([
    #("name", json.string(status.name)),
    #("status", json.string(types.status_to_string(status.status))),
    #("latency_ms", json.int(status.latency_ms)),
    #("last_check", json.string(birl.to_iso8601(status.last_check))),
    #("error", encode_optional_string(status.error)),
    #("history", json.array(status.history, encode_health_check)),
  ])
  |> json.to_string
}

fn encode_service_status_json(status: ServiceStatus) -> json.Json {
  json.object([
    #("status", json.string(types.status_to_string(status.status))),
    #("latency_ms", json.int(status.latency_ms)),
    #("last_check", json.string(birl.to_iso8601(status.last_check))),
    #("error", encode_optional_string(status.error)),
  ])
}

fn encode_health_check(check: HealthCheck) -> json.Json {
  json.object([
    #("status", json.string(types.status_to_string(check.status))),
    #("latency_ms", json.int(check.latency_ms)),
    #("checked_at", json.string(birl.to_iso8601(check.checked_at))),
    #("error", encode_optional_string(check.error)),
  ])
}

fn encode_optional_string(opt: option.Option(String)) -> json.Json {
  case opt {
    Some(s) -> json.string(s)
    None -> json.null()
  }
}
