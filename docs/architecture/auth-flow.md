# Authentication Flow

## OAuth Login Flow

```mermaid
sequenceDiagram
    participant User
    participant Gateway
    participant Auth as Auth Service
    participant Provider as OAuth Provider
    participant Redis

    User->>Gateway: GET /api/auth/oauth/{provider}
    Gateway->>Auth: Forward request
    Auth->>Redis: Store state token
    Auth-->>User: Redirect to provider

    User->>Provider: Authenticate
    Provider-->>User: Redirect with code

    User->>Gateway: GET /api/auth/oauth/{provider}/callback?code=xxx
    Gateway->>Auth: Forward callback
    Auth->>Redis: Validate state
    Auth->>Provider: Exchange code for token
    Provider-->>Auth: Access token + user info
    Auth->>Auth: Generate JWT (RSA-256)
    Auth->>Redis: Store session
    Auth-->>User: Set cookie + redirect
```

## Token Validation

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Auth as Auth Service
    participant Service as Protected Service

    Client->>Gateway: Request + Cookie
    Gateway->>Gateway: Extract JWT from cookie
    Gateway->>Auth: ValidateToken (gRPC)
    Auth-->>Gateway: Token claims
    Gateway->>Service: Request + claims header
    Service-->>Gateway: Response
    Gateway-->>Client: Response
```

## Token Refresh

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Auth as Auth Service
    participant Redis

    Client->>Gateway: POST /api/auth/refresh
    Gateway->>Auth: RefreshToken request
    Auth->>Redis: Validate refresh token
    Auth->>Auth: Generate new JWT pair
    Auth->>Redis: Update session
    Auth-->>Client: New tokens
```

## Account Linking Flow

Users can link additional OAuth providers to their account:

```mermaid
sequenceDiagram
    participant User
    participant Gateway
    participant Auth as Auth Service
    participant Provider as OAuth Provider

    User->>Gateway: GET /api/auth/oauth/{provider}/link
    Gateway->>Auth: Forward (with session cookie)
    Auth->>Auth: Validate existing session
    Auth->>Redis: Store link state
    Auth-->>User: Redirect to provider

    User->>Provider: Authorize
    Provider-->>User: Redirect with code

    User->>Gateway: GET /api/auth/oauth/{provider}/callback
    Gateway->>Auth: Forward callback
    Auth->>Auth: Detect link flow from state
    Auth->>Provider: Exchange code for token
    Auth->>Auth: Link account to existing user
    Auth-->>User: Redirect to settings
```

## JWT Structure

Tokens are signed with RSA-256. Keys stored at:
- Private: `./infra/keys/dev-private.pem`
- Public: `./infra/keys/dev-public.pem`

Claims include:
- `sub` - User ID
- `email` - User email
- `provider` - OAuth provider used
- `exp` - Expiration timestamp
