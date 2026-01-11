# Webhooks Worker

**Go**

Dispatches events to registered webhook endpoints with retry support and delivery tracking.

## Overview

| Property | Value |
|----------|-------|
| **Queue** | `webhooks-worker` |
| **Routing Key** | `hellarch.#` (all events) |
| **Database** | `events_db` → `webhooks`, `webhook_deliveries` |
| **Location** | `src/workers/webhooks/` |

## Architecture

```mermaid
graph TB
    subgraph RabbitMQ
        EX[hellarch.events]
        Q[webhooks-worker]
    end

    subgraph Webhooks Worker
        C[Consumer]
        D[Dispatcher]
        S[Store]
    end

    subgraph PostgreSQL
        DB[(events_db)]
        W[webhooks]
        WD[webhook_deliveries]
    end

    subgraph External
        E1[Endpoint 1]
        E2[Endpoint 2]
    end

    EX -->|hellarch.#| Q
    Q --> C
    C --> S
    S --> W
    C --> D
    D --> E1
    D --> E2
    D --> S
    S --> WD
    W --> DB
    WD --> DB
```

## How It Works

```mermaid
sequenceDiagram
    participant RMQ as RabbitMQ
    participant C as Consumer
    participant S as Store
    participant D as Dispatcher
    participant EP as External Endpoint

    RMQ->>C: Deliver message
    C->>C: Parse CloudEvent
    C->>S: GetWebhooksForEventType(type)
    S-->>C: [webhook1, webhook2]

    loop Each webhook
        C->>D: Deliver(request)
        D->>D: Compute HMAC signature
        D->>EP: POST with headers
        EP-->>D: Response
        D->>S: InsertDelivery(result)
    end

    C->>RMQ: Ack message
```

## Data Models

### Webhook

```go
type Webhook struct {
    ID         string         // UUID
    Name       string         // Display name
    URL        string         // Endpoint URL
    Secret     string         // HMAC signing key
    EventTypes []string       // Subscribed event types
    Format     WebhookFormat  // raw, discord, slack
    IsActive   bool           // Enable/disable
    CreatedAt  time.Time
    UpdatedAt  time.Time
}

type WebhookFormat string

const (
    WebhookFormatRaw     WebhookFormat = "raw"
    WebhookFormatDiscord WebhookFormat = "discord"
    WebhookFormatSlack   WebhookFormat = "slack"
)
```

### Webhook Delivery

```go
type WebhookDelivery struct {
    ID            string
    WebhookID     string
    EventID       string
    EventType     string
    Payload       []byte    // Full CloudEvent JSON
    StatusCode    *int      // HTTP response code
    ResponseBody  *string   // First 1KB of response
    Error         *string   // Error message if failed
    AttemptNumber int       // Retry attempt (1, 2, 3...)
    CreatedAt     time.Time
}
```

## Database Schema

```sql
CREATE TABLE webhooks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    url TEXT NOT NULL,
    secret TEXT NOT NULL,
    event_types TEXT[] NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE webhook_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    webhook_id UUID NOT NULL REFERENCES webhooks(id) ON DELETE CASCADE,
    event_id TEXT NOT NULL,
    event_type TEXT NOT NULL,
    payload JSONB NOT NULL,
    status_code INTEGER,
    response_body TEXT,
    error TEXT,
    attempt_number INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_webhook_deliveries_webhook_id ON webhook_deliveries(webhook_id);
CREATE INDEX idx_webhook_deliveries_created_at ON webhook_deliveries(created_at DESC);
```

## Payload Formats

Webhooks support three output formats to integrate with different services:

### Raw (CloudEvents)

Default format. Sends the original CloudEvents JSON:

```json
{
  "specversion": "1.0",
  "type": "hellarch.auth.user.created.v1",
  "source": "auth-service",
  "id": "evt_123",
  "time": "2024-01-15T10:30:00Z",
  "data": { "user_id": "..." }
}
```

### Discord

Transforms events into Discord webhook embeds with color-coded event types:

```json
{
  "embeds": [{
    "title": "User Created",
    "color": 3066993,
    "fields": [
      { "name": "User Id", "value": "...", "inline": true }
    ],
    "footer": { "text": "Source: auth-service" },
    "timestamp": "2024-01-15T10:30:00Z"
  }]
}
```

**Color Mapping:**
| Event Type | Color |
|------------|-------|
| `*.created` | Green (#2ECC71) |
| `*.deleted` | Red (#E74C3C) |
| `*.updated` | Blue (#3498DB) |
| `*.failed`, `*.unhealthy` | Red (#E74C3C) |
| `*.recovered` | Green (#2ECC71) |
| Other | Purple (#9B59B6) |

### Slack

Transforms events into Slack Block Kit format:

```json
{
  "text": "User Created",
  "blocks": [
    {
      "type": "header",
      "text": { "type": "plain_text", "text": "User Created" }
    },
    {
      "type": "section",
      "text": { "type": "mrkdwn", "text": "*Source:* auth-service\n*Event ID:* evt_123" }
    },
    {
      "type": "section",
      "fields": [
        { "type": "mrkdwn", "text": "*User Id:*\n..." }
      ]
    },
    {
      "type": "context",
      "elements": [{ "type": "mrkdwn", "text": "Received at 2024-01-15T10:30:00Z" }]
    }
  ]
}
```

## HTTP Delivery

### Request Format

```http
POST {webhook.url}
Content-Type: application/cloudevents+json
X-Webhook-Signature: sha256=abc123...
X-Event-ID: evt_123
X-Event-Type: hellarch.auth.user.created.v1

{
  "specversion": "1.0",
  "type": "hellarch.auth.user.created.v1",
  "source": "auth-service",
  "id": "evt_123",
  "time": "2024-01-15T10:30:00Z",
  "data": {
    "user_id": "...",
    "email": "..."
  }
}
```

### Signature Verification

Endpoints should verify the HMAC-SHA256 signature:

```go
// Compute expected signature
mac := hmac.New(sha256.New, []byte(webhookSecret))
mac.Write(requestBody)
expected := "sha256=" + hex.EncodeToString(mac.Sum(nil))

// Compare with header
if !hmac.Equal([]byte(expected), []byte(request.Header.Get("X-Webhook-Signature"))) {
    return errors.New("invalid signature")
}
```

### Response Handling

| Status Code | Result | Action |
|-------------|--------|--------|
| 2xx | Success | Delivery recorded as successful |
| 4xx | Client error | Delivery recorded as failed, no retry |
| 5xx | Server error | Delivery recorded as failed, may retry |
| Timeout | Error | Delivery recorded with error message |

## Configuration

| Constant | Value | Description |
|----------|-------|-------------|
| `RequestTimeout` | 30s | HTTP request timeout |
| `MaxRetries` | 3 | Maximum delivery attempts |

## Environment Variables

See [Workers Configuration](/guide/configuration#workers).

**Required:** `DATABASE_URL`, `RABBITMQ_URL`

## OpenTelemetry

Spans created:

- `webhooks.process_event` - Message processing (consumer)
- `webhook.deliver` - Individual delivery attempt (dispatcher)

Span attributes:
- `webhook.id`: Webhook UUID
- `webhook.url`: Endpoint URL
- `event.id`: CloudEvent ID
- `event.type`: Event type
- `attempt`: Delivery attempt number

## Docker

```yaml
webhooks-worker:
  build:
    context: ../../src/workers/webhooks
    dockerfile: Dockerfile
  environment:
    - DATABASE_URL=postgres://hellarch:hellarch@events-db:5432/events_db
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - OTEL_SERVICE_NAME=webhooks-worker
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```

## Code Structure

```
src/workers/webhooks/
├── cmd/worker/main.go              # Entry point
├── internal/
│   ├── config/config.go            # Environment loading
│   ├── consumer/consumer.go        # RabbitMQ consumer
│   ├── dispatcher/
│   │   ├── dispatcher.go           # HTTP delivery + signing
│   │   └── transformer.go          # Discord/Slack payload transform
│   ├── store/postgres.go           # Database operations
│   └── telemetry/telemetry.go      # OpenTelemetry setup
├── go.mod
├── go.sum
├── Dockerfile
└── .air.toml
```

## Admin UI

Webhooks are managed in the Admin Console at `/webhooks`:

- Create/edit/delete webhooks
- Enable/disable webhooks
- Select format (Raw, Discord, Slack)
- Event type catalog with domain grouping
- Select all / clear event subscriptions
- View deliveries tab with filtering:
  - Filter by webhook
  - Filter by success/failed
  - Pagination
- Delivery detail modal with response body
- Live updates via SignalR
