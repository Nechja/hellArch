# Audit Worker

**Go**

Consumes all events and stores them in an audit log for compliance and debugging.

## Overview

| Property | Value |
|----------|-------|
| **Queue** | `audit-worker` |
| **Routing Key** | `hellarch.#` (all events) |
| **Database** | `events_db` → `audit_logs` |
| **Location** | `src/workers/audit/` |

## Architecture

```mermaid
graph TB
    subgraph RabbitMQ
        EX[hellarch.events]
        Q[audit-worker]
    end

    subgraph Audit Worker
        C[Consumer]
        S[Store]
    end

    subgraph PostgreSQL
        DB[(events_db)]
        AL[audit_logs]
    end

    EX -->|hellarch.#| Q
    Q --> C
    C --> S
    S --> AL
    AL --> DB
```

## How It Works

```mermaid
sequenceDiagram
    participant RMQ as RabbitMQ
    participant C as Consumer
    participant S as Store
    participant DB as PostgreSQL

    RMQ->>C: Deliver message
    C->>C: Parse CloudEvent envelope
    C->>C: Extract actor_id, target_id
    C->>S: InsertAuditLog(log)
    S->>DB: INSERT INTO audit_logs
    DB-->>S: OK
    S-->>C: OK
    C->>RMQ: Ack message
```

## Data Model

```go
type AuditLog struct {
    ID        string    // UUID, auto-generated
    EventType string    // e.g., "hellarch.auth.user.created.v1"
    EventID   string    // CloudEvent ID
    Source    string    // e.g., "auth-service"
    ActorID   *string   // User who triggered the event
    TargetID  *string   // Entity affected by the event
    Metadata  []byte    // Full event payload (JSON)
    CreatedAt time.Time // Event timestamp
}
```

## Database Schema

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type TEXT NOT NULL,
    event_id TEXT NOT NULL,
    source TEXT NOT NULL,
    actor_id UUID,
    target_id UUID,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL,

    CONSTRAINT audit_logs_event_id_unique UNIQUE (event_id)
);

CREATE INDEX idx_audit_logs_event_type ON audit_logs(event_type);
CREATE INDEX idx_audit_logs_actor_id ON audit_logs(actor_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at DESC);
```

## Actor/Target Extraction

The worker extracts IDs from the event payload:

```go
// Actor: user who performed the action
func extractActorID(data json.RawMessage) *string {
    var m map[string]any
    json.Unmarshal(data, &m)
    if v, ok := m["user_id"].(string); ok {
        return &v
    }
    return nil
}

// Target: entity affected by the action
func extractTargetID(data json.RawMessage) *string {
    var m map[string]any
    json.Unmarshal(data, &m)
    if v, ok := m["session_id"].(string); ok {
        return &v
    }
    if v, ok := m["target_id"].(string); ok {
        return &v
    }
    return nil
}
```

## OpenTelemetry

Full distributed tracing with spans for:

- `audit.process_event` - Message processing
- Database queries via `otelpgx`

Span attributes:
- `messaging.system`: `rabbitmq`
- `messaging.operation.type`: `process`
- `messaging.cloudevents.type`: Event type

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DATABASE_URL` | Yes | - | PostgreSQL connection |
| `RABBITMQ_URL` | No | `amqp://guest:guest@localhost:5672` | RabbitMQ connection |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | `localhost:4318` | OTLP endpoint |

## Docker

```yaml
audit-worker:
  build:
    context: ../../src/workers/audit
    dockerfile: Dockerfile
  environment:
    - DATABASE_URL=postgres://hellarch:hellarch@events-db:5432/events_db
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - OTEL_SERVICE_NAME=audit-worker
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```

## Code Structure

```
src/workers/audit/
├── cmd/worker/main.go           # Entry point, signal handling
├── internal/
│   ├── config/config.go         # Environment loading
│   ├── consumer/consumer.go     # RabbitMQ consumer + event processing
│   ├── store/postgres.go        # Database operations
│   └── telemetry/telemetry.go   # OpenTelemetry setup
├── go.mod
├── go.sum
├── Dockerfile
└── .air.toml                    # Hot reload for development
```

## Admin UI

Audit logs are viewable in the Admin Console at `/audit`:

- Filter by event type
- Filter by actor ID
- Paginated results
- Expandable metadata view
