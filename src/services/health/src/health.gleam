import envoy
import gleam/erlang/process
import gleam/int
import gleam/io
import gleam/result
import health/config
import health/poller
import health/router
import mist

pub fn main() {
  let port = get_port()

  io.println("Starting health aggregator on port " <> int.to_string(port))

  // Start the poller
  let assert Ok(poller) = poller.start()

  // Start the HTTP server
  let assert Ok(_) =
    router.handler(poller)
    |> mist.new
    |> mist.port(port)
    |> mist.start

  io.println("Health aggregator running")

  // Keep the main process alive
  process.sleep_forever()
}

fn get_port() -> Int {
  envoy.get("PORT")
  |> result.try(int.parse)
  |> result.unwrap(config.default_port)
}
