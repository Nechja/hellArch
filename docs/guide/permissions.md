# Permissions & User Status

Role-based access control using a flexible permissions map stored as JSONB.

## How Permissions Flow

```mermaid
flowchart TB
    subgraph Admin["Admin Console"]
        ADMIN_UI[Admin UI]
    end

    subgraph AuthService["Auth Service"]
        ADMIN_API[Admin API]
        AUTH_API[Auth API]
    end

    subgraph Storage["PostgreSQL"]
        USERS[(users table<br/>permissions JSONB)]
    end

    subgraph Validation["Token Validation"]
        SESSION[Session Check]
        JWT_VAL[JWT Validation]
    end

    subgraph Consumer["Consuming Services"]
        GATEWAY[Gateway]
        APP[Application]
    end

    ADMIN_UI -->|"grant/revoke"| ADMIN_API
    ADMIN_API -->|"UPDATE jsonb"| USERS

    USERS -->|"SELECT permissions"| AUTH_API
    AUTH_API -->|"embed in JWT"| JWT_VAL
    AUTH_API -->|"return in response"| SESSION

    SESSION -->|"permissions: {...}"| GATEWAY
    JWT_VAL -->|"decode claims"| APP

    GATEWAY -->|"check permission"| ALLOW{Allow?}
    APP -->|"check permission"| ALLOW

    ALLOW -->|Yes| SUCCESS[200 OK]
    ALLOW -->|No| DENIED[403 Forbidden]
```

## Permission Lifecycle

```mermaid
sequenceDiagram
    participant Admin as Admin User
    participant API as Auth Admin API
    participant DB as PostgreSQL
    participant User as Target User
    participant GW as Gateway

    Note over Admin,GW: 1. Admin grants permission
    Admin->>API: POST /admin/users/{id}/permissions/grant<br/>{"permission": "reports.view"}
    API->>DB: UPDATE users SET permissions = jsonb_set(...)
    DB-->>API: OK
    API-->>Admin: 204 No Content

    Note over Admin,GW: 2. User authenticates
    User->>API: POST /auth/validate<br/>X-Session-Token: xxx
    API->>DB: SELECT permissions FROM users
    DB-->>API: {"reports.view": true}
    API-->>User: {"valid": true, "permissions": {"reports.view": true}}

    Note over Admin,GW: 3. User accesses protected resource
    User->>GW: GET /reports
    GW->>API: POST /auth/validate
    API-->>GW: {"permissions": {"reports.view": true}}
    GW->>GW: Check reports.view == true
    GW-->>User: 200 OK + Report Data
```

## User Deactivation Flow

```mermaid
sequenceDiagram
    participant Admin as Admin User
    participant API as Auth Admin API
    participant DB as PostgreSQL
    participant Redis as Redis Cache
    participant User as Deactivated User

    Admin->>API: POST /admin/users/{id}/deactivate
    API->>DB: UPDATE users SET is_active = false
    API->>DB: SELECT sessions WHERE user_id = {id}
    DB-->>API: [session1, session2, ...]

    loop Each Session
        API->>Redis: DELETE session:{token}
        API->>DB: DELETE FROM sessions WHERE id = {id}
    end

    API-->>Admin: 204 No Content

    Note over User: Later, user tries to access
    User->>API: POST /auth/validate<br/>X-Session-Token: xxx
    API->>Redis: GET session:xxx
    Redis-->>API: NOT FOUND
    API->>DB: SELECT * FROM sessions WHERE token = xxx
    DB-->>API: NOT FOUND
    API-->>User: 401 Unauthorized

    Note over User: User tries to refresh token
    User->>API: POST /auth/refresh<br/>{"refresh_token": "..."}
    API->>DB: SELECT is_active FROM users
    DB-->>API: is_active = false
    API-->>User: 403 Forbidden "user is inactive"
```

## Permission Storage

```mermaid
erDiagram
    users {
        uuid id PK
        text email UK
        text username
        boolean is_active "default: true"
        jsonb permissions "default: {}"
        timestamp created_at
        timestamp updated_at
    }

    users ||--o{ sessions : has
    users ||--o{ linked_accounts : has
```

**JSONB Structure:**
```json
{
  "admin.access": true,
  "admin.users.read": true,
  "admin.users.write": true,
  "reports.view": true,
  "reports.export": true
}
```

## Permission Check Flow

```mermaid
flowchart LR
    subgraph Request
        REQ[Incoming Request]
    end

    subgraph Auth["Auth Check"]
        VAL{Has Token?}
        VALID{Token Valid?}
        ACTIVE{User Active?}
        PERM{Has Permission?}
    end

    subgraph Response
        OK[200 OK]
        UNAUTH[401 Unauthorized]
        FORBIDDEN[403 Forbidden]
    end

    REQ --> VAL
    VAL -->|No| UNAUTH
    VAL -->|Yes| VALID
    VALID -->|No| UNAUTH
    VALID -->|Yes| ACTIVE
    ACTIVE -->|No| FORBIDDEN
    ACTIVE -->|Yes| PERM
    PERM -->|No| FORBIDDEN
    PERM -->|Yes| OK
```

## JWT with Permissions

```mermaid
flowchart LR
    subgraph Login["OAuth Login"]
        OAUTH[OAuth Callback]
        FETCH[Fetch User + Permissions]
    end

    subgraph Token["JWT Generation"]
        CLAIMS[Build Claims]
        SIGN[Sign with RS256]
    end

    subgraph JWT["JWT Token"]
        HEADER[Header]
        PAYLOAD[Payload with permissions]
        SIG[Signature]
    end

    OAUTH --> FETCH
    FETCH --> CLAIMS
    CLAIMS --> SIGN
    SIGN --> JWT

    PAYLOAD --- PERMS["permissions: {<br/>  admin.access: true<br/>}"]
```

**JWT Payload Example:**
```json
{
  "iss": "hellarch-auth",
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "exp": 1704067200,
  "uid": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "username": "testuser",
  "permissions": "{\"admin.access\":true,\"reports.view\":true}"
}
```

## Data Model

### User Entity

```csharp
public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, bool>? Permissions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Database Schema

```sql
-- Added via migration: AddUserPermissionsAndIsActive
ALTER TABLE users ADD COLUMN is_active boolean NOT NULL DEFAULT true;
ALTER TABLE users ADD COLUMN permissions jsonb DEFAULT '{}'::jsonb;
```

The permissions column uses PostgreSQL JSONB for flexible, queryable storage:

```json
{
  "admin.access": true,
  "users.read": true,
  "users.write": true,
  "reports.view": true
}
```

## Permission Format

Permissions use a dot-notation hierarchy:

| Permission | Description |
|------------|-------------|
| `admin.access` | Access to admin console |
| `admin.users.read` | View user list |
| `admin.users.write` | Modify users |
| `admin.sessions.revoke` | Revoke user sessions |
| `reports.view` | View reports |
| `reports.export` | Export reports |

Only `true` values are stored. Revoking removes the key entirely.

## Admin API

### Get User Permissions

```http
GET /auth/admin/users/{id}/permissions
```

Response:
```json
{
  "user_id": "550e8400-e29b-41d4-a716-446655440000",
  "permissions": {
    "admin.access": true,
    "users.read": true
  }
}
```

### Set All Permissions

Replace entire permissions map:

```http
PUT /auth/admin/users/{id}/permissions
Content-Type: application/json

{
  "permissions": {
    "admin.access": true,
    "users.read": true,
    "users.write": false
  }
}
```

### Grant Single Permission

```http
POST /auth/admin/users/{id}/permissions/grant
Content-Type: application/json

{
  "permission": "admin.users.write"
}
```

Response: `204 No Content`

### Revoke Single Permission

```http
POST /auth/admin/users/{id}/permissions/revoke
Content-Type: application/json

{
  "permission": "admin.users.write"
}
```

Response: `204 No Content`

## User Activation

### Deactivate User

```http
POST /auth/admin/users/{id}/deactivate
```

Response: `204 No Content`

**Side effects:**
- All active sessions are invalidated (deleted from DB + Redis)
- User cannot refresh tokens
- Session validation returns `403 Forbidden` with `user_inactive` error

### Activate User

```http
POST /auth/admin/users/{id}/activate
```

Response: `204 No Content`

## Auth Flow Integration

### Session Validation

`POST /auth/validate` returns permissions and active status:

```json
{
  "valid": true,
  "user_id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "username": "testuser",
  "is_active": true,
  "permissions": {
    "admin.access": true
  }
}
```

If user is inactive:
```json
{
  "valid": false,
  "error": "user_inactive"
}
```
Status: `403 Forbidden`

### JWT Token Claims

Permissions are embedded in JWT access tokens:

```json
{
  "iss": "hellarch-auth",
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "uid": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "username": "testuser",
  "permissions": "{\"admin.access\":true}",
  "exp": 1704067200
}
```

### Token Refresh

`POST /auth/refresh` checks `is_active` before issuing new tokens:

```mermaid
sequenceDiagram
    participant Client
    participant Auth as Auth Service
    participant DB as PostgreSQL

    Client->>Auth: POST /auth/refresh
    Auth->>DB: Get user by ID
    DB-->>Auth: User (with is_active)
    alt User is inactive
        Auth-->>Client: 401 "user is inactive"
    else User is active
        Auth->>Auth: Generate new tokens
        Auth-->>Client: 200 {access_token, refresh_token}
    end
```

## Database Operations

### PostgreSQL JSONB Functions

Permission operations use native JSONB functions:

```go
// Grant - add key with true value
`UPDATE users SET permissions = jsonb_set(
    COALESCE(permissions, '{}'::jsonb),
    $2,
    'true'::jsonb
), updated_at = NOW() WHERE id = $1`

// Revoke - remove key
`UPDATE users SET permissions = permissions - $2,
 updated_at = NOW() WHERE id = $1`
```

### Query Examples

```sql
-- Users with admin access
SELECT * FROM users
WHERE permissions->>'admin.access' = 'true';

-- Users with any permission starting with 'reports.'
SELECT * FROM users
WHERE permissions ?| array['reports.view', 'reports.export'];

-- Count users by permission
SELECT permissions->>'admin.access' as has_admin, COUNT(*)
FROM users
GROUP BY permissions->>'admin.access';
```

## Gateway Integration

The Gateway can use permissions for route authorization:

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Auth as Auth Service
    participant Admin as Admin Console

    Client->>Gateway: GET /admin/users
    Gateway->>Auth: POST /auth/validate
    Auth-->>Gateway: {valid: true, permissions: {...}}

    alt Has admin.access permission
        Gateway->>Admin: Forward request
        Admin-->>Gateway: Response
        Gateway-->>Client: 200 OK
    else No permission
        Gateway-->>Client: 403 Forbidden
    end
```

## Implementation Files

```
src/services/auth/
├── internal/
│   ├── handler/
│   │   ├── admin.go      # Permission endpoints
│   │   ├── auth.go       # Token generation with permissions
│   │   └── http.go       # Validate returns permissions
│   └── store/
│       └── postgres.go   # JSONB permission queries

libs/data_types/csharp/Auth/
└── User.cs               # Entity with Permissions field

src/schema/Schema/Migrations/Auth/
└── 20251228233227_AddUserPermissionsAndIsActive.cs
```
