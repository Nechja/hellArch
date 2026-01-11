import envoy
import gleam/erlang/process
import gleam/int
import gleam/result
import health/config
import health/events
import health/http_client
import health/poller
import health/registry
import health/router
import logging
import mist

pub fn main() {
  let port = get_port()

  logging.log(
    logging.Info,
    "Starting health aggregator on port " <> int.to_string(port),
  )

  // Wire up production dependencies
  let deps =
    poller.PollerDeps(
      load_services: registry.services,
      create_publisher: events.new,
      http_get: http_client.get,
      tcp_check: http_client.check_tcp_port,
    )

  // Start the poller with dependencies
  let assert Ok(poll) = poller.start_with_deps(deps)

  // Start the HTTP server
  let assert Ok(_) =
    router.handler(poll)
    |> mist.new
    |> mist.port(port)
    |> mist.bind("0.0.0.0")
    |> mist.start

  logging.log(logging.Info, "Health aggregator running")

  process.sleep_forever()
}

fn get_port() -> Int {
  envoy.get("PORT")
  |> result.try(int.parse)
  |> result.unwrap(config.default_port)
}
