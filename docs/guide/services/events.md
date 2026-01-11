# Events Service

**Go**

Central API for event management, audit logs, webhooks, and worker monitoring.

## Overview

| Property | Value |
|----------|-------|
| **Port** | 5005 |
| **Database** | `events_db` |
| **Location** | `src/services/events/` |

## Architecture

```mermaid
graph TB
    subgraph Events Service :5005
        API[HTTP API]
        PUB[RabbitMQ Publisher]
        MON[Worker Monitor]
        ST[(Store)]
    end

    subgraph RabbitMQ
        EX[hellarch.events]
        MGMT[Management API]
    end

    subgraph Database
        DB[(events_db)]
        AL[audit_logs]
        WH[webhooks]
        WD[webhook_deliveries]
        DL[dead_letters]
    end

    subgraph Consumers
        ADMIN[Admin Console]
        WORKERS[Workers]
    end

    ADMIN -->|REST| API
    WORKERS -->|REST| API
    API --> ST
    ST --> DB
    API --> PUB
    PUB --> EX
    MON --> MGMT
    MON --> PUB
```

## API Reference

See [Events API](/api/events) for complete endpoint documentation.

**Key endpoints:**
- `/api/events/audit-logs` - Audit log CRUD
- `/api/events/webhooks` - Webhook management
- `/api/events/webhook-deliveries` - Delivery tracking
- `/api/events/dead-letters` - Failed event management
- `/api/events/workers/status` - Worker queue status
- `/api/events/publish` - Event publishing

## Worker Monitor

The service includes a background worker monitor that:

1. Polls RabbitMQ Management API every 10 seconds
2. Checks queue consumer counts
3. Emits `hellarch.infra.service.unhealthy.v1` when a queue has 0 consumers
4. Emits `hellarch.infra.service.recovered.v1` when consumers return

This enables the Infra Worker to auto-restart unhealthy workers.

```mermaid
sequenceDiagram
    participant M as Worker Monitor
    participant RMQ as RabbitMQ Mgmt
    participant PUB as Publisher

    loop Every 10 seconds
        M->>RMQ: GET /api/queues
        RMQ-->>M: Queue stats
        M->>M: Check consumer counts

        alt Queue has 0 consumers
            M->>PUB: Publish service.unhealthy
        else Queue recovered
            M->>PUB: Publish service.recovered
        end
    end
```

## Database Schema

See [Events Database](/guide/database#events-database) for full schema.

**Tables:** `audit_logs`, `webhooks`, `webhook_deliveries`, `dead_letters`

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DATABASE_URL` | Yes | - | PostgreSQL connection string |
| `RABBITMQ_URL` | No | - | RabbitMQ AMQP URL for publishing |
| `RABBITMQ_MANAGEMENT_URL` | No | - | RabbitMQ Management API URL |
| `HTTP_ADDRESS` | No | `:5005` | HTTP listen address |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | - | OpenTelemetry endpoint |
| `LOG_LEVEL` | No | `info` | Logging level |

## Code Structure

```
src/services/events/
├── cmd/server/main.go           # Entry point
├── internal/
│   ├── config/config.go         # Environment config
│   ├── handler/
│   │   ├── http.go              # Route registration
│   │   ├── audit.go             # Audit log handlers
│   │   ├── webhooks.go          # Webhook CRUD handlers
│   │   ├── deliveries.go        # Delivery handlers
│   │   ├── deadletters.go       # Dead letter handlers
│   │   ├── publish.go           # Event publishing
│   │   └── workers.go           # Worker status
│   ├── store/
│   │   ├── postgres.go          # DB connection
│   │   ├── audit.go             # Audit queries
│   │   ├── webhooks.go          # Webhook queries
│   │   ├── deliveries.go        # Delivery queries
│   │   └── deadletters.go       # Dead letter queries
│   └── monitor/
│       └── worker_monitor.go    # Worker health monitoring
├── go.mod
├── go.sum
└── Dockerfile
```

## Docker

```yaml
events:
  build:
    context: ../../src/services/events
    dockerfile: Dockerfile
  ports:
    - "5005:5005"
  environment:
    - DATABASE_URL=postgres://hellarch:hellarch@events-db:5432/events_db
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - RABBITMQ_MANAGEMENT_URL=http://hellarch:hellarch@rabbitmq:15672
    - OTEL_SERVICE_NAME=events
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```

## Admin Console Integration

The Admin Console uses `EventsAdminFacade` to interact with this service:

- `/audit` - Browse audit logs
- `/webhooks` - Manage webhooks and view deliveries
- `/events` - View worker status and dead letters
