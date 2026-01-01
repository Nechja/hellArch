# User Service

**F# / .NET 10**

Manages user profiles and preferences using functional patterns.

## Architecture

```mermaid
graph TB
    subgraph User Service
        API[HTTP API :5002]
        DOMAIN[Domain Types]
        INFRA[Infrastructure]
    end

    API --> DOMAIN
    DOMAIN --> INFRA
    INFRA --> PG[(PostgreSQL)]
```

## Domain Model

```fsharp
type UserProfile = {
    Id: Guid
    Email: string
    DisplayName: string option
    AvatarUrl: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type Preferences = {
    UserId: Guid
    Theme: Theme        // Light | Dark | System
    Locale: string
    NotificationsEnabled: bool
}

type LinkedAccount = {
    UserId: Guid
    Provider: OAuthProvider
    ProviderUserId: string
    LinkedAt: DateTime
}
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/profile/{id}` | Get user profile |
| POST | `/profile` | Create profile |
| PUT | `/profile/{id}` | Update profile |
| GET | `/profile/{id}/preferences` | Get preferences |
| PUT | `/profile/{id}/preferences` | Update preferences |
| GET | `/profile/{id}/accounts` | List linked accounts |
| POST | `/profile/{id}/accounts` | Link account |
| DELETE | `/profile/{id}/accounts/{provider}` | Unlink account |

## Event Consuming

Consumes CloudEvents from RabbitMQ to react to auth state changes.

```mermaid
graph LR
    subgraph RabbitMQ
        EX[hellarch.events]
        Q[user.events]
    end

    subgraph User Service
        C[Consumer]
        H[Handler]
        DB[(PostgreSQL)]
    end

    EX -->|hellarch.auth.user.#| Q
    Q --> C
    C --> H
    H --> DB
```

### Consumed Events

| Event | Action |
|-------|--------|
| `hellarch.auth.user.created.v1` | Create UserProfile |

### Idempotency

Consumer checks for existing profile before creating:

```fsharp
match existingProfile with
| Ok(Some _) -> () // Skip
| Ok None -> Database.createProfile dbConfig request
```

### OpenTelemetry Tracing

Consumer propagates trace context from RabbitMQ headers:

```mermaid
sequenceDiagram
    participant Auth as Auth Service
    participant RMQ as RabbitMQ
    participant User as User Service

    Auth->>RMQ: Publish with traceparent
    RMQ->>User: Deliver message
    User->>User: Extract trace context
    Note over User: Start span: rabbitmq.consume
    User->>User: Process event
    Note over User: Set span attributes
```

```fsharp
let private activitySource = new ActivitySource("hellarch/user/events")

// Extract trace context from headers
let parentContext = extractTraceContext headers

// Start span with parent context
use activity =
    activitySource.StartActivity(
        "rabbitmq.consume",
        ActivityKind.Consumer,
        parentContext.ActivityContext
    )

// Set semantic convention tags
activity.SetTag("messaging.system", "rabbitmq")
activity.SetTag("messaging.destination.name", "hellarch.events")
activity.SetTag("messaging.cloudevents.type", eventType)
```

## Code Structure

```
src/services/user/
└── User/
    ├── Program.fs           # Entry point
    ├── Domain/
    │   └── Types.fs         # Domain types
    ├── Infrastructure/
    │   ├── Database.fs      # Npgsql.FSharp access
    │   └── Events.fs        # RabbitMQ consumer
    └── Api/
        └── Handlers.fs      # HTTP handlers
```

## Database

Uses dedicated PostgreSQL instance on port 5433.

## Environment

```bash
USER_DATABASE_URL="postgres://hellarch:hellarch@localhost:5433/user_db"
RABBITMQ_URL="amqp://guest:guest@localhost:5672/"
USER_EVENTS_QUEUE="user.events"
OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4318"
```

## Location

`src/services/user/`
