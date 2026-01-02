# Realtime Worker

**C# (.NET 10)**

Consumes events from RabbitMQ and broadcasts them to connected browser clients via SignalR WebSockets.

## Overview

| Property | Value |
|----------|-------|
| **Queue** | `realtime-worker` |
| **Routing Key** | `hellarch.#` (all events) |
| **Hub Endpoint** | `/hubs/events` |
| **Port** | `5020` |
| **Location** | `src/workers/realtime/` |

## Architecture

```mermaid
graph LR
    subgraph Services
        AUTH[Auth]
        USER[User]
    end

    subgraph RabbitMQ
        EX[hellarch.events]
        Q[realtime-worker]
    end

    subgraph Realtime Worker
        C[EventConsumer]
        H[EventsHub]
    end

    subgraph Browsers
        B1[User Browser]
        B2[Admin Browser]
    end

    AUTH -->|publish| EX
    USER -->|publish| EX
    EX -->|hellarch.#| Q
    Q --> C
    C --> H
    H -->|WebSocket| B1
    H -->|WebSocket| B2
```

## How It Works

```mermaid
sequenceDiagram
    participant S as Auth Service
    participant RMQ as RabbitMQ
    participant C as EventConsumer
    participant H as EventsHub
    participant B as Browser

    Note over B,H: Connection Phase
    B->>H: Connect with JWT
    H->>H: Validate token
    H->>H: Add to groups (user:123, all)
    H-->>B: Connected

    Note over S,B: Event Flow
    S->>RMQ: Publish user.updated
    RMQ->>C: Deliver message
    C->>C: Parse CloudEvent
    C->>H: Broadcast to user:123
    H->>B: ReceiveEvent(event)
    B->>B: Update UI
    C->>RMQ: Ack message
```

## SignalR Groups

Clients are automatically added to groups on connection:

| Group | Pattern | Who Joins | Use Case |
|-------|---------|-----------|----------|
| `all` | Static | All users | Broadcast messages |
| `user:{id}` | `user:abc-123` | Single user | User-specific events |
| `admins` | Static | Admin role | Admin-only events |
| `event:{type}` | `event:hellarch.auth.user.created.v1` | Subscribers | Opt-in to specific types |

## Client Interface

Methods the server can call on connected clients:

```typescript
interface IEventsClient {
  // Receive any CloudEvent
  receiveEvent(event: CloudEvent): void;

  // Session was invalidated - redirect to login
  sessionInvalidated(reason: string): void;

  // User data changed - refetch profile
  userUpdated(): void;
}
```

## Event Routing

When an event arrives, the consumer broadcasts to relevant groups:

```csharp
// 1. Send to event-type subscribers
await hubContext.Clients
    .Group($"event:{eventType}")
    .ReceiveEvent(cloudEvent);

// 2. Send to user-specific group
if (userId is not null)
{
    await hubContext.Clients
        .Group($"user:{userId}")
        .ReceiveEvent(cloudEvent);
}

// 3. Special handlers for specific events
if (eventType.Contains("session.ended"))
{
    await hubContext.Clients
        .Group($"user:{userId}")
        .SessionInvalidated(reason);
}

// 4. Admin events to admin group
if (IsAdminEvent(eventType))
{
    await hubContext.Clients
        .Group("admins")
        .ReceiveEvent(cloudEvent);
}
```

## Frontend Usage

### TypeScript/JavaScript

```typescript
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('/hubs/events', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .configureLogging(LogLevel.Information)
  .build();

// Handle incoming events
connection.on('ReceiveEvent', (event) => {
  console.log('Event:', event.type, event.data);

  switch (event.type) {
    case 'hellarch.auth.user.updated.v1':
      refreshUserProfile();
      break;
    case 'hellarch.auth.session.created.v1':
      showNotification('New login detected');
      break;
  }
});

// Handle session invalidation
connection.on('SessionInvalidated', (reason) => {
  console.log('Session ended:', reason);
  window.location.href = '/login';
});

// Handle user updates
connection.on('UserUpdated', () => {
  refetchUserProfile();
});

// Start connection
await connection.start();

// Optional: Subscribe to specific event types
await connection.invoke('SubscribeToEventType', 'hellarch.auth.user.created.v1');
```

### Blazor

```csharp
@inject NavigationManager Navigation
@implements IAsyncDisposable

@code {
    private HubConnection? _hub;

    protected override async Task OnInitializedAsync()
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/events"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(GetToken());
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<CloudEventDto>("ReceiveEvent", OnEventReceived);
        _hub.On<string>("SessionInvalidated", OnSessionInvalidated);
        _hub.On("UserUpdated", OnUserUpdated);

        await _hub.StartAsync();
    }

    private void OnEventReceived(CloudEventDto evt)
    {
        // Handle event
        InvokeAsync(StateHasChanged);
    }

    private void OnSessionInvalidated(string reason)
    {
        Navigation.NavigateTo("/login", forceLoad: true);
    }

    private void OnUserUpdated()
    {
        // Refetch user data
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
```

## Authentication

SignalR WebSocket connections can't send HTTP headers, so JWT is passed via query string:

```
wss://localhost:5000/hubs/events?access_token=eyJhbG...
```

The worker extracts and validates the token:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) &&
            path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }
};
```

## CloudEvent DTO

```csharp
public sealed record CloudEventDto
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset Time { get; init; }
    public JsonElement? Data { get; init; }
}
```

## OpenTelemetry

Distributed tracing with spans for:

- `realtime.process_event` - Message processing

Span attributes:
- `messaging.system`: `rabbitmq`
- `messaging.operation.type`: `process`
- `messaging.cloudevents.type`: Event type

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `RABBITMQ_URL` | Yes | - | RabbitMQ connection string |
| `JWT_SECRET` | Yes | - | JWT signing key |
| `JWT_ISSUER` | No | `hellarch` | Token issuer |
| `JWT_AUDIENCE` | No | `hellarch` | Token audience |
| `CORS_ORIGINS` | No | `localhost:5000,5010` | Allowed origins (comma-separated) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | `localhost:4318` | OTLP endpoint |
| `LOG_LEVEL` | No | `debug` | Logging level |

## Docker

```yaml
realtime-worker:
  build:
    context: ../../src/workers/realtime
    dockerfile: Dockerfile
    target: dev
  ports:
    - "5020:5020"
  env_file:
    - ../../.env
    - ../../.env.secrets
  environment:
    - ASPNETCORE_URLS=http://+:5020
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - CORS_ORIGINS=http://localhost:5000,http://localhost:5010
    - OTEL_SERVICE_NAME=realtime-worker
    - OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4318
```

## Code Structure

```
src/workers/realtime/
├── Program.cs                    # Entry point, DI setup
├── Hubs/
│   ├── EventsHub.cs              # SignalR hub, group management
│   └── IEventsClient.cs          # Typed client interface, DTOs
├── Services/
│   └── EventConsumer.cs          # RabbitMQ consumer, broadcasting
├── appsettings.json
├── appsettings.Development.json
├── Realtime.csproj
└── Dockerfile
```

## Mise Tasks

```bash
mise run realtime:build    # Build the project
mise run realtime:run      # Run locally
mise run realtime:watch    # Hot reload development
mise run realtime:publish  # Production build
```

## Gateway Integration

The gateway routes SignalR connections to the realtime worker:

```json
{
  "hubs-route": {
    "ClusterId": "realtime-cluster",
    "Match": {
      "Path": "/hubs/{**catch-all}"
    }
  }
}
```

YARP automatically handles WebSocket upgrade requests.
