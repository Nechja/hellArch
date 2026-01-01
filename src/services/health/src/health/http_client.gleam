import gleam/dynamic.{type Dynamic}

pub type HttpResult {
  HttpOk(status: Int, body: String)
  HttpError(reason: String)
}

@external(erlang, "health_http_ffi", "get")
fn do_http_get(url: String, timeout_ms: Int) -> Dynamic

pub fn get(url: String, timeout_ms: Int) -> HttpResult {
  let result = do_http_get(url, timeout_ms)
  decode_result(result)
}

fn decode_result(dyn: Dynamic) -> HttpResult {
  // The Erlang FFI returns either {ok, StatusCode, Body} or {error, Reason}
  // We use unsafe coercion since we control the Erlang side
  case decode_ok(dyn) {
    Ok(#(status, body)) -> HttpOk(status, body)
    Error(_) ->
      case decode_error(dyn) {
        Ok(reason) -> HttpError(reason)
        Error(_) -> HttpError("Unknown error")
      }
  }
}

@external(erlang, "health_http_ffi", "decode_ok")
fn decode_ok(dyn: Dynamic) -> Result(#(Int, String), Nil)

@external(erlang, "health_http_ffi", "decode_error")
fn decode_error(dyn: Dynamic) -> Result(String, Nil)

@external(erlang, "health_tcp_ffi", "check_port")
pub fn check_tcp_port(host: String, port: Int, timeout_ms: Int) -> Bool
