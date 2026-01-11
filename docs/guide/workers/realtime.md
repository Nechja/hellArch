# Realtime Worker

**C# / ASP.NET Core / SignalR**

Broadcasts events to connected browser clients in real-time via WebSockets.

## Overview

| Property | Value |
|----------|-------|
| **Port** | 5005 |
| **Queue** | `realtime-worker` |
| **Routing Key** | `hellarch.#` (all events) |
| **Hub Endpoint** | `/hubs/events` |
| **Location** | `src/workers/realtime/` |

## Architecture

```mermaid
graph TB
    subgraph RabbitMQ
        EX[hellarch.events]
        Q[realtime-worker]
    end

    subgraph Realtime Worker
        C[EventConsumer]
        H[EventsHub]
    end

    subgraph Clients
        B1[Browser 1]
        B2[Browser 2]
        B3[Admin Console]
    end

    EX -->|hellarch.#| Q
    Q --> C
    C --> H
    H -->|WebSocket| B1
    H -->|WebSocket| B2
    H -->|WebSocket| B3
```

## How It Works

```mermaid
sequenceDiagram
    participant RMQ as RabbitMQ
    participant EC as EventConsumer
    participant Hub as EventsHub
    participant Client as Browser

    Note over Client,Hub: Client connects with JWT
    Client->>Hub: Connect(/hubs/events?access_token=xxx)
    Hub->>Hub: Validate JWT
    Hub->>Hub: Add to groups (user:xxx, all)
    Hub-->>Client: Connected

    Note over RMQ,Client: Event broadcast flow
    RMQ->>EC: Deliver CloudEvent
    EC->>EC: Parse event, extract userId
    EC->>Hub: Broadcast to groups
    Hub->>Client: ReceiveEvent(cloudEvent)
```

## SignalR Groups

Clients are automatically organized into groups for targeted delivery:

| Group | Pattern | Description |
|-------|---------|-------------|
| `all` | All authenticated | Broadcast events |
| `user:{userId}` | Per-user | User-specific events |
| `admins` | Admin role | Admin-only events |
| `event:{type}` | Per-event-type | Subscribed event types |

## Client Interface

```typescript
// TypeScript client example
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/events", {
        accessTokenFactory: () => getAccessToken()
    })
    .withAutomaticReconnect()
    .build();

// Receive any CloudEvent
connection.on("ReceiveEvent", (event: CloudEvent) => {
    console.log("Event received:", event.type, event.data);
});

// Session was invalidated (redirect to login)
connection.on("SessionInvalidated", (reason: string) => {
    console.log("Session ended:", reason);
    window.location.href = "/login";
});

// User data changed (refresh cached data)
connection.on("UserUpdated", () => {
    console.log("User data updated");
    refreshUserProfile();
});

await connection.start();

// Subscribe to specific event types
await connection.invoke("SubscribeToEventType", "hellarch.auth.session.created.v1");
```

## Server Methods

Methods clients can invoke on the hub:

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToEventType` | `eventType: string` | Join event-type specific group |
| `UnsubscribeFromEventType` | `eventType: string` | Leave event-type specific group |

## Client Methods

Methods the server can invoke on clients:

| Method | Parameters | Description |
|--------|------------|-------------|
| `ReceiveEvent` | `CloudEventDto` | Receive a CloudEvent |
| `SessionInvalidated` | `reason: string` | Session was ended |
| `UserUpdated` | - | User profile changed |

## Event Routing

```mermaid
flowchart TD
    E[CloudEvent] --> T{Event Type?}

    T -->|session.ended| SI[SessionInvalidated]
    T -->|user.updated| UU[UserUpdated]
    T -->|user.created/deleted| AG[Admin Group]
    T -->|any| ET[Event Type Group]
    T -->|has userId| UG[User Group]

    SI --> UC[User's Clients]
    UU --> UC
    AG --> AC[Admin Clients]
    ET --> SC[Subscribed Clients]
    UG --> UC
```

## Authentication

JWT tokens are validated on connection:

```csharp
// Token sent via query string for WebSocket upgrade
/hubs/events?access_token=eyJhbGciOiJIUzI1NiIs...

// Token validation parameters
ValidateIssuerSigningKey = true
ValidIssuer = "hellarch"
ValidAudience = "hellarch"
ValidateLifetime = true
ClockSkew = 1 minute
```

## Environment Variables

See [Workers Configuration](/guide/configuration#workers).

**Required:** `RABBITMQ_URL`, `JWT_SECRET`

## Docker

```yaml
realtime-worker:
  build:
    context: ../../src/workers/realtime
    dockerfile: Dockerfile
  ports:
    - "5005:8080"
  environment:
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - JWT_SECRET=${JWT_SECRET}
    - CORS_ORIGINS=http://localhost:5000,http://localhost:5010
    - OTEL_SERVICE_NAME=realtime-worker
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```

## Code Structure

```
src/workers/realtime/
├── Program.cs                 # Entry point, DI, middleware
├── Hubs/
│   ├── EventsHub.cs           # SignalR hub with group management
│   └── IEventsClient.cs       # Typed client interface + DTOs
├── Services/
│   └── EventConsumer.cs       # RabbitMQ consumer, broadcasts to hub
├── appsettings.json
├── appsettings.Development.json
├── Realtime.csproj
└── Dockerfile
```

## OpenTelemetry

Tracing spans created:

- `realtime.process_event` - Message processing

Span attributes:
- `messaging.system`: `rabbitmq`
- `messaging.operation.type`: `process`
- `messaging.cloudevents.type`: Event type

## Health Check

```http
GET /health
```

Returns `200 OK` when the service is running.

## CORS

SignalR requires specific CORS configuration for WebSocket connections:

```csharp
policy
    .WithOrigins(corsOrigins)  // Specific origins only
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();       // Required for SignalR
```

## Reconnection

Clients should use automatic reconnection:

```typescript
const connection = new signalR.HubConnectionBuilder()
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();

connection.onreconnecting(() => {
    showReconnectingUI();
});

connection.onreconnected(() => {
    hideReconnectingUI();
    resubscribeToEvents();
});
```
