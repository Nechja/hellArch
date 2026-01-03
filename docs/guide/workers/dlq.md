# DLQ Worker

**C# / ASP.NET Core**

Consumes failed events from the Dead Letter Queue for logging and monitoring.

## Overview

| Property | Value |
|----------|-------|
| **Queue** | `hellarch.events.dlq` |
| **Exchange** | `hellarch.events.dlq` (fanout) |
| **Location** | `src/workers/dlq/` |

## Architecture

```mermaid
graph TB
    subgraph RabbitMQ
        Q1[audit-worker]
        Q2[webhooks-worker]
        Q3[realtime-worker]
        DLX[hellarch.events.dlq]
        DLQ[hellarch.events.dlq]
    end

    subgraph DLQ Worker
        C[DlqConsumer]
        L[Logger]
    end

    Q1 -->|nack/timeout| DLX
    Q2 -->|nack/timeout| DLX
    Q3 -->|nack/timeout| DLX
    DLX --> DLQ
    DLQ --> C
    C --> L
```

## How It Works

When a message fails to process in any worker queue (nack, timeout, or rejection), RabbitMQ routes it to the Dead Letter Exchange. The DLQ Worker:

1. Consumes messages from `hellarch.events.dlq`
2. Extracts failure metadata from headers
3. Logs comprehensive error details
4. Acknowledges the message (prevents infinite loops)

```mermaid
sequenceDiagram
    participant W as Other Worker
    participant RMQ as RabbitMQ
    participant DLQ as DLQ Worker
    participant Log as Logger

    W->>RMQ: nack(message)
    RMQ->>RMQ: Route to DLX
    RMQ->>DLQ: Deliver dead letter
    DLQ->>DLQ: Extract headers
    DLQ->>Log: LogError with details
    DLQ->>RMQ: ack(message)
```

## Dead Letter Headers

RabbitMQ attaches metadata to dead-lettered messages:

| Header | Description |
|--------|-------------|
| `x-first-death-exchange` | Original exchange |
| `x-first-death-queue` | Original queue |
| `x-first-death-reason` | Why it was dead-lettered |
| `ce-type` | CloudEvent type |
| `ce-id` | CloudEvent ID |
| `ce-source` | CloudEvent source |

## Log Output

```json
{
  "level": "Error",
  "message": "Dead letter received",
  "EventType": "hellarch.auth.session.created.v1",
  "EventId": "evt_123",
  "EventSource": "auth-service",
  "OriginalQueue": "webhooks-worker",
  "DeathReason": "rejected",
  "Body": "{...truncated...}"
}
```

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `RABBITMQ_URL` | Yes | - | RabbitMQ connection string |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | `localhost:4318` | OTLP endpoint |
| `LOG_LEVEL` | No | `debug` | Logging level |

## Docker

```yaml
dlq-worker:
  build:
    context: ../../src/workers/dlq
    dockerfile: Dockerfile
  environment:
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - OTEL_SERVICE_NAME=dlq-worker
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```

## Code Structure

```
src/workers/dlq/
├── Program.cs                # Entry point, DI setup
├── Services/
│   └── DlqConsumer.cs        # Background service, consumes DLQ
├── appsettings.json
├── DlqWorker.csproj
└── Dockerfile
```

## Future Enhancements

The worker includes TODO comments for planned features:

- Store failed events in a database for manual retry
- Send alerts for specific event types
- Implement automatic retry with exponential backoff
- Publish metrics for monitoring dashboards

## Health Check

```http
GET /health
```

Returns `200 OK` when the service is running.
