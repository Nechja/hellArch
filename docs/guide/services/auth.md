# Auth Service

**Go 1.24**

Handles all authentication concerns.

## Architecture

```mermaid
graph TB
    subgraph Auth Service
        HTTP[HTTP :5001]
        GRPC[gRPC :50051]
        OAUTH[OAuth Handler]
        JWT[JWT Manager]
        STORE[Store Layer]
    end

    HTTP --> OAUTH
    HTTP --> JWT
    GRPC --> JWT
    OAUTH --> STORE
    JWT --> STORE
    STORE --> PG[(PostgreSQL)]
    STORE --> REDIS[(Redis)]
```

## OAuth Providers

Supported providers configured via environment:

| Provider | Env Vars |
|----------|----------|
| Discord | `DISCORD_CLIENT_ID`, `DISCORD_CLIENT_SECRET` |
| Google | `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` |
| GitHub | `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET` |
| Microsoft | `MICROSOFT_CLIENT_ID`, `MICROSOFT_CLIENT_SECRET` |

## Endpoints

### HTTP (Port 5001)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/oauth/{provider}` | Start OAuth flow |
| GET | `/oauth/{provider}/callback` | OAuth callback |
| POST | `/refresh` | Refresh tokens |
| POST | `/logout` | End session |
| GET | `/health` | Health check |

### Admin API (Internal)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/admin/users` | List users (paginated) |
| GET | `/admin/users/{id}` | Get user with linked accounts |
| GET | `/admin/users/{id}/sessions` | Get user's sessions |
| DELETE | `/admin/sessions/{id}` | Revoke a session |
| GET | `/admin/stats` | Get system statistics |
| GET | `/admin/users/{id}/permissions` | Get user permissions |
| PUT | `/admin/users/{id}/permissions` | Set all permissions |
| POST | `/admin/users/{id}/permissions/grant` | Grant permission |
| POST | `/admin/users/{id}/permissions/revoke` | Revoke permission |
| POST | `/admin/users/{id}/activate` | Activate user |
| POST | `/admin/users/{id}/deactivate` | Deactivate user + invalidate sessions |

See [Permissions Guide](/guide/permissions) for details.

### gRPC (Port 50051)

```protobuf
service AuthService {
  rpc ValidateToken(ValidateTokenRequest) returns (ValidateTokenResponse);
  rpc GetSession(GetSessionRequest) returns (SessionResponse);
  rpc EndSession(EndSessionRequest) returns (EndSessionResponse);
}
```

## JWT Configuration

Tokens signed with RSA-256:

```bash
JWT_PRIVATE_KEY_PATH="./infra/keys/dev-private.pem"
JWT_PUBLIC_KEY_PATH="./infra/keys/dev-public.pem"
```

## Event Publishing

Publishes CloudEvents to RabbitMQ when authentication state changes.

```mermaid
graph LR
    subgraph Auth Service
        OAUTH[OAuth Handler]
        PUB[Publisher]
    end

    subgraph RabbitMQ
        EX[hellarch.events]
    end

    OAUTH -->|UserCreated| PUB
    OAUTH -->|SessionCreated| PUB
    PUB --> EX
```

### Published Events

| Event | Trigger |
|-------|---------|
| `hellarch.auth.user.created.v1` | New user via OAuth |
| `hellarch.auth.session.created.v1` | Login/token refresh |
| `hellarch.auth.session.ended.v1` | Logout |
| `hellarch.auth.account.linked.v1` | OAuth account linked |

### OpenTelemetry Tracing

Publisher includes trace context in AMQP headers for distributed tracing.

```go
otel.GetTextMapPropagator().Inject(ctx, amqpCarrier(headers))
```

## Code Structure

```
src/services/auth/
├── cmd/server/          # Entry point
├── internal/
│   ├── config/          # Configuration loading
│   ├── events/          # CloudEvents publisher
│   ├── handler/         # HTTP + gRPC handlers
│   ├── jwt/             # Token generation/validation
│   ├── oauth/           # Provider implementations
│   └── store/           # PostgreSQL + Redis access
└── go.mod
```

## Environment

```bash
RABBITMQ_URL="amqp://guest:guest@localhost:5672/"
```

## Location

`src/services/auth/`
