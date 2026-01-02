import health/types.{type Service, HttpGet, RabbitMqConsumer, Service, TcpConnect}

pub fn services() -> List(Service) {
  [
    Service(
      name: "gateway",
      url: "http://gateway:5000",
      check_type: HttpGet("/health"),
      critical: True,
    ),
    Service(
      name: "auth",
      url: "http://auth:5001",
      check_type: HttpGet("/auth/health"),
      critical: True,
    ),
    Service(
      name: "user",
      url: "http://user:5002",
      check_type: HttpGet("/user/health"),
      critical: True,
    ),
    Service(
      name: "admin",
      url: "http://admin:5003",
      check_type: HttpGet("/health"),
      critical: False,
    ),
    Service(
      name: "events",
      url: "http://events:5005",
      check_type: HttpGet("/health"),
      critical: True,
    ),
    Service(
      name: "redis",
      url: "redis:6379",
      check_type: TcpConnect,
      critical: True,
    ),
    Service(
      name: "auth-db",
      url: "auth-db:5432",
      check_type: TcpConnect,
      critical: True,
    ),
    Service(
      name: "user-db",
      url: "user-db:5432",
      check_type: TcpConnect,
      critical: True,
    ),
    Service(
      name: "rabbitmq",
      url: "rabbitmq:5672",
      check_type: TcpConnect,
      critical: True,
    ),
    Service(
      name: "events-db",
      url: "events-db:5432",
      check_type: TcpConnect,
      critical: True,
    ),
    // Workers - check RabbitMQ consumer count
    Service(
      name: "audit-worker",
      url: "",
      check_type: RabbitMqConsumer("audit-worker"),
      critical: False,
    ),
    Service(
      name: "webhooks-worker",
      url: "",
      check_type: RabbitMqConsumer("webhooks-worker"),
      critical: False,
    ),
    Service(
      name: "infra-worker",
      url: "",
      check_type: RabbitMqConsumer("infra-events"),
      critical: False,
    ),
  ]
}
