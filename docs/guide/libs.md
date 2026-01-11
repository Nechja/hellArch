# Shared Libraries

Internal libraries shared across services. Located in `libs/`.

## Go Libraries

Located in `libs/go/`. Import as `github.com/nechja/hellarch/libs/go/{package}`.

| Package | Purpose |
|---------|---------|
| `config` | Environment variable loading |
| `events` | CloudEvents client |
| `health` | Health check HTTP handler |
| `httpclient` | Configured HTTP client with tracing |
| `httputil` | Client IP extraction, JSON responses |
| `logging` | Structured logging setup (slog) |
| `postgres` | Connection pool management |
| `rabbitmq` | RabbitMQ connection, consumer, publisher |
| `server` | Graceful shutdown handling |
| `status` | Service status types |
| `telemetry` | OpenTelemetry setup |

### Example Usage

```go
import (
    "github.com/nechja/hellarch/libs/go/health"
    "github.com/nechja/hellarch/libs/go/logging"
    "github.com/nechja/hellarch/libs/go/httputil"
)

func main() {
    logging.Setup()

    mux := http.NewServeMux()
    health.Register(mux)  // Adds /health endpoint

    // In handlers
    clientIP := httputil.ClientIP(r)
    httputil.JSON(w, http.StatusOK, data)
}
```

## C# Libraries

Located in `libs/csharp/`. Published as NuGet packages.

| Package | Purpose |
|---------|---------|
| `HellArch.Http` | Resilient HTTP handler, service registration |
| `HellArch.RabbitMQ` | RabbitMQ connection factory, consumer base |

### HellArch.Http

Provides `ResilientHttpHandler` with automatic retry and structured logging:

```csharp
services.AddResilientHttpClients(config =>
{
    config.AddClient("auth", "http://auth:5001");
    config.AddClient("user", "http://user:5002");
});
```

### HellArch.RabbitMQ

Provides `IRabbitMqConnectionFactory` and `RabbitMqConsumerBase`:

```csharp
services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();

public class MyConsumer : RabbitMqConsumerBase
{
    protected override Task HandleMessage(string message) { ... }
}
```

## F# Libraries

Located in `libs/fsharp/`.

| Package | Purpose |
|---------|---------|
| `HellArch.HttpUtil` | HTTP response helpers, JSON serialization |

### HellArch.HttpUtil

Standard JSON response helpers for F# services:

```fsharp
open HellArch.HttpUtil

let handler : HttpHandler =
    fun next ctx ->
        jsonResponse ctx { Status = "ok" }
```

## Data Types

Cross-language type definitions in `libs/data_types/`. See [Generated Types](/types/) documentation.

## Design System

CSS utilities in `libs/design/`. See the design system source for available classes.
