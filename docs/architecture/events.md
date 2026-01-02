# CloudEvents

Event-driven communication using CloudEvents 1.0 spec with JSON payloads over RabbitMQ.

## Architecture

```mermaid
graph LR
    subgraph Publishers
        AUTH[Auth Service<br/>Go]
    end

    subgraph Broker
        EX[hellarch.events<br/>topic exchange]
        Q1[user.events queue]
        Q2[events-worker queue]
    end

    subgraph Subscribers
        USER[User Service<br/>F#]
        WORKER[Events Worker<br/>Rust]
    end

    AUTH -->|publish| EX
    EX -->|hellarch.auth.user.#| Q1
    EX -->|hellarch.#| Q2
    Q1 --> USER
    Q2 --> WORKER
```

## RabbitMQ Configuration

```mermaid
graph TB
    subgraph Exchange
        EX[hellarch.events<br/>type: topic<br/>durable: true]
    end

    subgraph Queues
        Q1[user.events]
        Q2[events-worker]
    end

    subgraph Routing Keys
        RK1[hellarch.auth.user.created.v1]
        RK2[hellarch.auth.user.updated.v1]
        RK3[hellarch.auth.session.created.v1]
    end

    EX -->|hellarch.auth.user.#| Q1
    EX -->|hellarch.#| Q2
```

| Queue | Binding | Consumer | Purpose |
|-------|---------|----------|---------|
| `user.events` | `hellarch.auth.user.#` | F# User Service | Create user profiles |
| `events-worker` | `hellarch.#` | Rust Events Worker | Emails, analytics, audit |

## Event Structure

CloudEvents 1.0 envelope with JSON payload. Headers carry CloudEvents metadata for routing.

```mermaid
graph TB
    subgraph AMQP Message
        subgraph Headers
            H1[ce-specversion: 1.0]
            H2[ce-id: uuid]
            H3[ce-source: /auth]
            H4[ce-type: hellarch.auth.user.created.v1]
            H5[traceparent: OpenTelemetry]
        end
        subgraph Body
            JSON[CloudEvent JSON]
        end
    end
```

### CloudEvent JSON Format

```json
{
  "specversion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "source": "/auth",
  "type": "hellarch.auth.user.created.v1",
  "datacontenttype": "application/json",
  "time": "2024-01-15T10:30:00Z",
  "data": {
    "user_id": "...",
    "email": "...",
    "username": "..."
  }
}
```

## Event Types

| Type | Payload | Description |
|------|---------|-------------|
| `hellarch.auth.user.created.v1` | `UserCreated` | New user via OAuth |
| `hellarch.auth.user.updated.v1` | `UserUpdated` | User info changed |
| `hellarch.auth.user.deleted.v1` | `UserDeleted` | User removed |
| `hellarch.auth.session.created.v1` | `SessionCreated` | New session |
| `hellarch.auth.session.ended.v1` | `SessionEnded` | Logout/expiry |
| `hellarch.auth.account.linked.v1` | `AccountLinked` | OAuth linked |
| `hellarch.auth.account.unlinked.v1` | `AccountUnlinked` | OAuth unlinked |

## Payloads

### UserCreated

```json
{
  "user_id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "username": "username",
  "provider": "github",
  "provider_user_id": "12345",
  "avatar_url": "https://avatars.githubusercontent.com/u/12345",
  "created_at": "2024-01-15T10:30:00Z"
}
```

### SessionCreated

```json
{
  "session_id": "550e8400-e29b-41d4-a716-446655440000",
  "user_id": "550e8400-e29b-41d4-a716-446655440001",
  "ip_address": "192.168.1.1",
  "user_agent": "Mozilla/5.0...",
  "created_at": "2024-01-15T10:30:00Z",
  "expires_at": "2024-01-16T10:30:00Z"
}
```

### AccountLinked

```json
{
  "user_id": "550e8400-e29b-41d4-a716-446655440000",
  "provider": "discord",
  "provider_user_id": "67890",
  "email": "user@example.com",
  "linked_at": "2024-01-15T10:30:00Z"
}
```

## Event Flow

```mermaid
sequenceDiagram
    participant Auth as Auth Service
    participant RMQ as RabbitMQ
    participant User as User Service
    participant DB as User DB

    Auth->>Auth: User signs up via OAuth
    Auth->>RMQ: Publish UserCreated
    Note over RMQ: routing key: hellarch.auth.user.created.v1
    RMQ->>User: Deliver to user.events queue
    User->>User: Parse CloudEvent
    User->>DB: Check existing profile
    alt Profile exists
        User->>User: Skip (idempotent)
    else New user
        User->>DB: Create UserProfile
    end
    User->>RMQ: ACK message
```

## Go Publisher

The Auth service publishes events using a Go publisher with OpenTelemetry tracing.

```mermaid
graph LR
    subgraph Auth Service
        H[OAuth Handler]
        P[Publisher]
        T[Tracer]
    end

    subgraph RabbitMQ
        CH[Channel]
        EX[Exchange]
    end

    H -->|event data| P
    P --> T
    T -->|inject trace context| P
    P -->|PublishWithContext| CH
    CH --> EX
```

**Key Features:**
- CloudEvents 1.0 compliant envelope
- OpenTelemetry trace context propagation via AMQP headers
- Topic exchange with event type as routing key
- Persistent message delivery mode

**Location:** `src/services/auth/internal/events/publisher.go`

## F# Consumer

The User service consumes events with idempotency handling.

```mermaid
graph TB
    subgraph User Service
        C[AsyncEventingBasicConsumer]
        H[Event Handler]
        DB[(User DB)]
    end

    subgraph Processing
        P[Parse CloudEvent]
        CHK[Check Existing]
        CR[Create Profile]
    end

    C -->|ReceivedAsync| H
    H --> P
    P --> CHK
    CHK -->|exists| ACK[BasicAck]
    CHK -->|new| CR
    CR --> DB
    DB --> ACK
```

**Key Features:**
- Binds to `hellarch.auth.user.#` wildcard pattern
- Idempotency check before creating profiles
- Manual ACK after successful processing
- NACK with requeue on errors

**Location:** `src/services/user/User/Infrastructure/Events.fs`

## Rust Consumer

The Events Worker handles side effects like emails and analytics.

```mermaid
graph TB
    subgraph Events Worker
        C[Consumer]
        M[Match event type]
    end

    subgraph Handlers
        H1[handle_user_created]
        H2[handle_session_created]
        H3[handle_account_linked]
    end

    subgraph Side Effects
        E[Welcome email]
        A[Analytics tracking]
        AU[Audit logging]
    end

    C --> M
    M -->|user.created| H1
    M -->|session.created| H2
    M -->|account.linked| H3
    H1 --> E
    H1 --> A
    H2 --> AU
```

**Key Features:**
- Binds to `hellarch.#` to receive all events
- Tokio async runtime
- Structured JSON logging with tracing
- Handles user, session, and account events

**Location:** `src/workers/events/`

## Tracing

OpenTelemetry trace context flows through events:

```mermaid
sequenceDiagram
    participant Client
    participant Auth as Auth Service
    participant RMQ as RabbitMQ
    participant User as User Service

    Client->>Auth: OAuth callback
    Note over Auth: Start span: oauth.callback
    Auth->>Auth: Create user
    Note over Auth: Start span: rabbitmq.publish
    Auth->>RMQ: Publish with traceparent header
    RMQ->>User: Deliver event
    Note over User: Extract traceparent
    Note over User: Continue trace
```

The publisher injects trace context into AMQP headers using `otel.GetTextMapPropagator().Inject()`.

## Proto Definitions

Event payloads are defined in protobuf for documentation and potential gRPC integration:

```protobuf
// libs/protos/events.proto

message UserCreated {
    string user_id = 1;
    string email = 2;
    string username = 3;
    string provider = 4;
    string provider_user_id = 5;
    string avatar_url = 6;
    google.protobuf.Timestamp created_at = 7;
}

message SessionEnded {
    string session_id = 1;
    string user_id = 2;
    SessionEndReason reason = 3;
    google.protobuf.Timestamp ended_at = 4;
}

enum SessionEndReason {
    SESSION_END_REASON_UNSPECIFIED = 0;
    SESSION_END_REASON_LOGOUT = 1;
    SESSION_END_REASON_EXPIRED = 2;
    SESSION_END_REASON_REVOKED = 3;
}
```

Note: Runtime uses JSON serialization, not protobuf binary. Proto serves as schema documentation.

## Adding New Events

```mermaid
graph TB
    subgraph 1. Define Source
        CS[Add C# class in libs/cloud_events/csharp/]
    end

    subgraph 2. Generate
        GEN[Run mise run codegen:events]
    end

    subgraph 3. Publish
        CONST[Add EventType constant]
        METHOD[Add Publish method]
    end

    subgraph 4. Consume
        HANDLER[Add handler in consumer]
    end

    CS --> GEN --> CONST --> METHOD --> HANDLER
```

1. **Define the payload** in `libs/cloud_events/csharp/`:
   ```csharp
   namespace HellArch.CloudEvents.Auth;

   public class MyEvent
   {
       public required string UserId { get; init; }
       public required DateTimeOffset HappenedAt { get; init; }
   }
   ```

2. **Run codegen** to generate types for all languages:
   ```bash
   mise run codegen:events
   ```

3. **Add event type constant** in `publisher.go`:
   ```go
   const EventTypeMyEvent = "hellarch.auth.myevent.v1"
   ```

4. **Add type alias and publish method** in `publisher.go`:
   ```go
   type MyEvent = auth.MyEvent

   func (p *Publisher) PublishMyEvent(ctx context.Context, data MyEvent) error {
       return p.Publish(ctx, EventTypeMyEvent, data, "")
   }
   ```

5. **Add consumer handler** using generated types from `hellarch_cloud_events` (Rust) or `HellArch.CloudEvents` (F#)

## Error Handling

```mermaid
graph TB
    subgraph Publisher
        PUB[Publish]
        ERR1[Marshal error]
        ERR2[Channel error]
    end

    subgraph Consumer
        RCV[Receive]
        ERR3[Parse error]
        ERR4[Handler error]
    end

    PUB -->|success| OK1[Message delivered]
    PUB -->|fail| ERR1 --> SPAN1[Record span error]
    PUB -->|fail| ERR2 --> SPAN1

    RCV -->|success| OK2[ACK message]
    RCV -->|parse fail| ERR3 --> NACK1[NACK + requeue]
    RCV -->|handler fail| ERR4 --> NACK1
```

| Scenario | Publisher | Consumer |
|----------|-----------|----------|
| Marshal failure | Record error span, return | N/A |
| Publish failure | Record error span, return | N/A |
| Parse failure | N/A | Log, NACK with requeue |
| Handler failure | N/A | Log, NACK with requeue |
| Duplicate event | N/A | Idempotency check, ACK |

## Source Locations

```
libs/protos/
└── events.proto              # Event payload definitions

src/services/auth/internal/events/
└── publisher.go              # Go CloudEvents publisher

src/services/user/User/Infrastructure/
└── Events.fs                 # F# event consumer

src/workers/events/
├── src/
│   ├── main.rs               # Rust consumer + queue setup
│   └── handlers.rs           # Event handlers
├── Cargo.toml
└── Dockerfile
```
