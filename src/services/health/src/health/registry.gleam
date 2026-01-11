import gleam/dynamic/decode
import gleam/io
import gleam/json
import gleam/list
import gleam/result
import gleam/string
import health/config
import health/types.{type Service, HttpGet, Service, TcpConnect}
import simplifile

type ServiceConfig {
  ServiceConfig(
    name: String,
    url: String,
    check_type: String,
    health_path: String,
    critical: Bool,
  )
}

pub fn services() -> List(Service) {
  let config_path = config.get_config_path()

  case load_services(config_path) {
    Ok(svcs) -> svcs
    Error(reason) -> {
      panic as { "Failed to load services config: " <> reason }
    }
  }
}

fn load_services(path: String) -> Result(List(Service), String) {
  use content <- result.try(
    simplifile.read(path)
    |> result.map_error(fn(_) { "Failed to read config file: " <> path }),
  )

  use configs <- result.try(parse_config(content))
  use services <- result.try(validate_and_convert(configs))

  Ok(services)
}

fn parse_config(content: String) -> Result(List(ServiceConfig), String) {
  let service_decoder = {
    use name <- decode.field("name", decode.string)
    use url <- decode.field("url", decode.string)
    use check_type <- decode.field("checkType", decode.string)
    use health_path <- decode.optional_field("healthPath", "", decode.string)
    use critical <- decode.field("critical", decode.bool)
    decode.success(ServiceConfig(name, url, check_type, health_path, critical))
  }

  let config_decoder = {
    use svcs <- decode.field("services", decode.list(service_decoder))
    decode.success(svcs)
  }

  case json.parse(content, config_decoder) {
    Ok(svcs) -> Ok(svcs)
    Error(err) -> Error("JSON parse error: " <> json_error_to_string(err))
  }
}

fn json_error_to_string(err: json.DecodeError) -> String {
  case err {
    json.UnexpectedEndOfInput -> "Unexpected end of input"
    json.UnexpectedByte(byte) -> "Unexpected byte: " <> byte
    json.UnexpectedSequence(seq) -> "Unexpected sequence: " <> seq
    json.UnableToDecode(errs) -> {
      let error_strings =
        list.map(errs, fn(e) {
          e.expected <> " at " <> string.join(e.path, "/")
        })
      "Unable to decode: " <> string.join(error_strings, ", ")
    }
  }
}

fn validate_and_convert(
  configs: List(ServiceConfig),
) -> Result(List(Service), String) {
  list.try_map(configs, config_to_service)
}

fn config_to_service(cfg: ServiceConfig) -> Result(Service, String) {
  use check_type <- result.try(case cfg.check_type {
    "http" -> {
      case cfg.health_path {
        "" ->
          Error(
            "Service \""
            <> cfg.name
            <> "\" has checkType \"http\" but missing healthPath",
          )
        path -> Ok(HttpGet(path))
      }
    }
    "tcp" -> Ok(TcpConnect)
    unknown -> {
      io.println(
        "Warning: Unknown checkType \""
        <> unknown
        <> "\" for service \""
        <> cfg.name
        <> "\", defaulting to tcp",
      )
      Ok(TcpConnect)
    }
  })

  Ok(Service(
    name: cfg.name,
    url: cfg.url,
    check_type: check_type,
    critical: cfg.critical,
  ))
}
