# Data Storage

## Database Architecture

Each service owns its database - no shared databases. Migrations baked into DB containers.

```mermaid
graph TB
    subgraph Auth Database Container
        AUTH_PG[(PostgreSQL :5434)]
    end

    subgraph User Database Container
        USER_PG[(PostgreSQL :5433)]
    end

    subgraph Events Database Container
        EVENTS_PG[(PostgreSQL)]
    end

    subgraph Files Database Container
        FILES_PG[(PostgreSQL)]
    end

    subgraph Identity Database Container
        IDENTITY_PG[(PostgreSQL :5437)]
    end

    subgraph Services
        AUTH[Auth Service]
        USER[User Service]
        EVENTS[Events Service]
        FILES[Files Service]
        IDENTITY[Identity Service]
    end

    subgraph Caching
        REDIS[(Redis :6379)]
    end

    subgraph Messaging
        RABBIT[(RabbitMQ :5672)]
    end

    subgraph Object Storage
        MINIO[(MinIO)]
    end

    AUTH --> AUTH_PG
    AUTH --> REDIS
    AUTH --> RABBIT
    USER --> USER_PG
    EVENTS --> EVENTS_PG
    EVENTS --> RABBIT
    FILES --> FILES_PG
    FILES --> MINIO
    FILES --> RABBIT
    IDENTITY --> IDENTITY_PG
    RABBIT --> USER
```

## Auth Database

Stores authentication data:
- Sessions
- Refresh tokens
- OAuth provider tokens

## User Database

Stores user data:
- Profiles (id, email, display_name, avatar_url)
- Preferences (theme, locale, notifications)
- Linked accounts (provider, provider_user_id)

## Events Database

Stores event and webhook data:
- Audit logs (event tracking)
- Webhooks (configuration)
- Webhook deliveries (attempt history)
- Dead letters (failed events)

## Files Database

Stores file metadata:
- Stored files (filename, mime_type, size, object_key)
- File variants (thumbnails, processed versions)

## Identity Database

Stores OAuth 2.0 authorization server data:
- Identity users (email, username, password_hash)
- OAuth clients (client_id, secret_hash, redirect_uris)
- Authorization codes (PKCE support)
- Access tokens (SHA-256 hashed)
- Refresh tokens (family tracking, rotation)

## MinIO Object Storage

Stores actual file content:
- Original uploads in `hellarch-files` bucket
- Generated variants (thumbnails, etc.)

## Redis Usage

```mermaid
graph LR
    subgraph Redis Keys
        STATE["oauth:state:*"]
        SESSION["session:*"]
        TOKEN["token:*"]
    end

    STATE -->|TTL 10m| A[OAuth state validation]
    SESSION -->|TTL 24h| B[Active session data]
    TOKEN -->|TTL 1h| C[Token blacklist/validation]
```

## Connection Strings

From `.envrc`:
```bash
AUTH_DATABASE_URL="postgres://hellarch:hellarch@localhost:5434/auth_db"
USER_DATABASE_URL="postgres://hellarch:hellarch@localhost:5433/user_db"
DATABASE_URL="postgres://hellarch:hellarch@localhost:5432/events_db"  # Events
FILES_DATABASE_URL="postgres://hellarch:hellarch@localhost:5432/files_db"
IDENTITY_DATABASE_URL="postgres://hellarch:hellarch@localhost:5437/identity_db"
REDIS_URL="redis://localhost:6379"
MINIO_ENDPOINT="localhost:9000"
```
