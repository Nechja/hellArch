# Identity Service

**F# (.NET 10)**

OAuth 2.0 authorization server with user registration and authentication.

## Overview

| Property | Value |
|----------|-------|
| **Port** | 5010 |
| **Tech** | F# / ASP.NET Core 10 |
| **Location** | `src/services/identity/` |

## Architecture

```mermaid
graph TB
    subgraph Identity Service :5010
        AUTH[Authorization Handler]
        TOKEN[Token Handler]
        USER[UserInfo Handler]
        DB[Database Layer]
        HASH[Password Hasher]
        BREACH[Breach Detection]
    end

    subgraph Clients
        APP[App Client]
        AUTHSVC[Auth Service]
    end

    APP -->|/authorize| AUTH
    APP -->|/login, /register| AUTH
    AUTHSVC -->|/token| TOKEN
    AUTHSVC -->|/userinfo| USER
    AUTH --> DB
    AUTH --> HASH
    AUTH --> BREACH
    TOKEN --> DB
    USER --> DB
    DB --> PG[(PostgreSQL)]
```

## OAuth 2.0 Flow

```mermaid
sequenceDiagram
    participant User
    participant App as App Client
    participant Identity as Identity Service
    participant Auth as Auth Service
    participant DB as PostgreSQL

    User->>App: Click "Login"
    App->>Identity: GET /authorize?client_id=...&code_challenge=...
    Identity->>Identity: Validate client, create session
    Identity-->>User: Redirect to login page

    User->>Identity: POST /login (email, password)
    Identity->>DB: Verify credentials
    Identity->>Identity: Generate authorization code
    Identity-->>User: Redirect to callback with code

    User->>Auth: GET /callback?code=xxx
    Auth->>Identity: POST /token (code, code_verifier)
    Identity->>DB: Validate code, PKCE
    Identity->>DB: Create access + refresh tokens
    Identity-->>Auth: Token response
    Auth-->>User: Set session cookie
```

## Endpoints

::: tip Gateway Routing
Clients access these endpoints via the Gateway at `/api/identity/...`. The Gateway strips the `/api` prefix before forwarding.
:::

| Method | Path | Description |
|--------|------|-------------|
| GET | `/identity/authorize` | Start OAuth authorization flow |
| POST | `/identity/login` | Authenticate user, issue authorization code |
| POST | `/identity/register` | Register user, issue authorization code |
| POST | `/identity/token` | Exchange code or refresh token for tokens |
| POST | `/identity/revoke` | Revoke access or refresh token |
| GET | `/identity/userinfo` | Get authenticated user info (OIDC) |
| GET | `/identity/health` | Health check |

### Authorization Request

```http
GET /identity/authorize?client_id=xxx&redirect_uri=xxx&response_type=code&code_challenge=xxx&code_challenge_method=S256&state=xxx
```

Validates the OAuth client and redirects to the login UI.

### Token Request

```http
POST /identity/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&code=xxx&redirect_uri=xxx&client_id=xxx&client_secret=xxx&code_verifier=xxx
```

**Response:**
```json
{
  "access_token": "...",
  "refresh_token": "...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

### Refresh Token

```http
POST /identity/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=xxx&client_id=xxx&client_secret=xxx
```

### UserInfo

```http
GET /identity/userinfo
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "username": "username",
  "avatar_url": null
}
```

## Security Features

### Password Security

- **Argon2id** hashing (memory-hard, resistant to GPU/ASIC attacks)
- Salt: 16 bytes, Hash: 32 bytes
- Memory: 64 MB, Iterations: 3, Parallelism: 4
- Constant-time comparison prevents timing attacks

### PKCE Support

Proof Key for Code Exchange (RFC 7636) prevents authorization code interception:

- Only S256 (SHA-256) method supported
- Code verifier validated during token exchange
- Required for all authorization flows

### Token Security

- Tokens stored as SHA-256 hashes (raw tokens never persisted)
- Client fingerprint binding (IP + User-Agent hash)
- Stolen tokens from different client rejected

### Token Rotation

```mermaid
flowchart LR
    RT1[Refresh Token 1] -->|used| RT2[Refresh Token 2]
    RT1 -->|marked revoked| DB[(Database)]
    RT1 -->|reused?| REVOKE[Revoke entire family]
```

- Refresh tokens marked revoked after use (not deleted)
- Family tracking: reuse detection revokes all tokens in family
- Prevents refresh token theft going unnoticed

### Breach Detection

Integrates with Have I Been Pwned API:

- k-Anonymity: only 5-char SHA-1 prefix sent to API
- Non-blocking: API failures don't prevent registration
- Warns users if password found in breaches

## Token Lifetimes

| Token Type | Lifetime |
|------------|----------|
| Authorization Code | 5 minutes |
| Access Token | 1 hour |
| Refresh Token | 30 days |

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `HTTP_PORT` | `5010` | HTTP listen port |
| `IDENTITY_DATABASE_URL` | - | PostgreSQL connection string |
| `IDENTITY_CLIENT_URL` | `http://localhost:5000/identity` | Frontend login/register page URL |
| `IDENTITY_ALLOWED_ORIGINS` | `http://localhost:5000` | CORS allowed origins |
| `IDENTITY_SEED_CLIENT_ID` | - | OAuth client ID to seed on startup |
| `IDENTITY_SEED_CLIENT_SECRET` | - | OAuth client secret to seed |
| `IDENTITY_SEED_REDIRECT_URIS` | - | Comma-separated redirect URIs |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | - | OpenTelemetry collector endpoint |

## Database Schema

The Identity service uses its own PostgreSQL database (`identity-db`).

### Tables

**identity_users**
- `id` (uuid, PK)
- `email` (unique)
- `username`
- `password_hash` (Argon2id)
- `is_active`
- `created_at`, `updated_at`

**oauth_clients**
- `id` (text, PK)
- `secret_hash` (Argon2id)
- `name`
- `redirect_uris` (array)
- `is_active`

**authorization_codes**
- `code` (text, PK)
- `client_id`, `user_id`
- `redirect_uri`, `scope`
- `code_challenge`, `code_challenge_method`
- `expires_at`

**access_tokens** / **refresh_tokens**
- `token_hash` (SHA-256, PK)
- `user_id`, `client_id`
- `scope`, `expires_at`
- `client_fingerprint`
- `family_id` (refresh tokens only)
- `is_revoked` (refresh tokens only)

## Code Structure

```
src/services/identity/
├── Dockerfile
└── Identity/
    ├── Identity.fsproj
    ├── Program.fs              # Entry point, DI setup
    ├── Api/
    │   ├── AuthHandlers.fs     # /authorize, /login, /register
    │   ├── TokenHandlers.fs    # /token, /revoke
    │   ├── ResourceHandlers.fs # /userinfo
    │   ├── HealthHandlers.fs   # /health
    │   ├── Config.fs           # Configuration types
    │   ├── HttpUtils.fs        # Error handling
    │   └── Views.fs            # URL builders
    ├── Domain/
    │   ├── Types.fs            # Domain models, errors
    │   └── Validation.fs       # Input validation
    └── Infrastructure/
        ├── Database.fs         # PostgreSQL operations
        ├── PasswordHasher.fs   # Argon2id hashing
        ├── TokenGenerator.fs   # Secure token generation
        └── BreachDetection.fs  # HIBP integration
```

## Integration with Auth Service

The Auth service uses Identity as its OAuth provider for the "hellarch" provider:

```mermaid
sequenceDiagram
    participant User
    participant Auth as Auth Service
    participant Identity as Identity Service

    User->>Auth: GET /oauth/hellarch
    Auth->>Identity: Redirect to /authorize
    Note over Identity: User logs in
    Identity-->>Auth: Callback with code
    Auth->>Identity: POST /token
    Identity-->>Auth: Access + refresh tokens
    Auth->>Identity: GET /userinfo
    Identity-->>Auth: User claims
    Auth->>Auth: Create session
```

## Client Seeding

On startup, if `IDENTITY_SEED_CLIENT_ID` and `IDENTITY_SEED_CLIENT_SECRET` are set, the service creates an OAuth client automatically. This is idempotent - existing clients are not modified.

```bash
IDENTITY_SEED_CLIENT_ID=hellarch
IDENTITY_SEED_CLIENT_SECRET=secret
IDENTITY_SEED_REDIRECT_URIS=http://localhost:5000/api/auth/hellarch/callback
```

## Dependencies

| Package | Purpose |
|---------|---------|
| Npgsql.FSharp | PostgreSQL access |
| Konscious.Security.Cryptography.Argon2 | Password hashing |
| OpenTelemetry.* | Distributed tracing |
| HellArch.HttpUtil | Shared HTTP utilities |
| HellArch.DataTypes | Shared type definitions |
