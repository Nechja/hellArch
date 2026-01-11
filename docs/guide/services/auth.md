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
| Apple | `APPLE_CLIENT_ID`, `APPLE_TEAM_ID`, `APPLE_KEY_ID`, `APPLE_PRIVATE_KEY` |
| Twitch | `TWITCH_CLIENT_ID`, `TWITCH_CLIENT_SECRET` |
| HellArch | `HELLARCH_CLIENT_ID`, `HELLARCH_CLIENT_SECRET`, `HELLARCH_IDENTITY_URL` |

::: tip HellArch Provider
The `hellarch` provider uses the [Identity Service](/guide/services/identity) as an OAuth provider, enabling local username/password authentication.
:::

## Linked Accounts

Users can link multiple OAuth providers to a single account. Linking happens automatically when a user starts an OAuth flow while already authenticated.

```mermaid
sequenceDiagram
    participant User
    participant App
    participant Auth as Auth Service
    participant Provider as OAuth Provider

    User->>App: Click "Link {provider}"
    App->>Auth: GET /auth/{provider}/login
    Auth->>Auth: Detect existing session
    Auth-->>User: Redirect to provider

    User->>Provider: Authorize
    Provider-->>Auth: Callback with code
    Auth->>Auth: Link account to existing user
    Auth-->>User: Redirect to settings
```

**Linking flow:**
1. User must be authenticated (valid session cookie)
2. Starting OAuth flow (`/auth/{provider}/login`) detects the session
3. After callback, account is linked to current user instead of creating new session
4. User can now sign in with any linked provider

**Get current user and linked providers:**
```http
GET /auth/me
Cookie: hellarch_session=xxx
```

**Response:**
```json
{
  "user_id": "550e8400-e29b-41d4-a716-446655440000",
  "linked_providers": ["discord", "github"]
}
```

## Endpoints

### HTTP (Port 5001)

::: tip Gateway Routing
Clients access these endpoints via the Gateway at `/api/auth/...`. The Gateway strips the `/api` prefix before forwarding.
:::

| Method | Path | Description |
|--------|------|-------------|
| GET | `/auth/{provider}/login` | Start OAuth flow |
| GET | `/auth/{provider}/callback` | OAuth callback |
| POST | `/auth/refresh` | Refresh tokens |
| POST | `/auth/validate` | Validate session or JWT |
| GET | `/auth/me` | Get current user + linked providers |
| GET | `/auth/providers` | List configured OAuth providers |
| GET/POST | `/auth/logout` | End session |
| GET | `/auth/health` | Health check |

### Admin API (Internal)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/auth/admin/users` | List users (paginated) |
| GET | `/auth/admin/users/{id}` | Get user with linked accounts |
| GET | `/auth/admin/users/{id}/sessions` | Get user's sessions |
| DELETE | `/auth/admin/sessions/{id}` | Revoke a session |
| GET | `/auth/admin/stats` | Get system statistics |
| GET | `/auth/admin/users/{id}/permissions` | Get user permissions |
| PUT | `/auth/admin/users/{id}/permissions` | Set all permissions |
| POST | `/auth/admin/users/{id}/permissions/grant` | Grant permission |
| POST | `/auth/admin/users/{id}/permissions/revoke` | Revoke permission |
| POST | `/auth/admin/users/{id}/activate` | Activate user |
| POST | `/auth/admin/users/{id}/deactivate` | Deactivate user + invalidate sessions |

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
│   ├── proto/           # gRPC generated code
│   ├── service/         # Business logic (user, session, permission)
│   ├── store/           # PostgreSQL + Redis access
│   └── telemetry/       # OpenTelemetry setup
└── go.mod
```

## Environment

```bash
RABBITMQ_URL="amqp://guest:guest@localhost:5672/"
```

## Location

`src/services/auth/`
