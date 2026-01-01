# Data Storage

## Database Architecture

Each service owns its database - no shared databases. Migrations baked into DB containers.

```mermaid
graph TB
    subgraph Auth Database Container
        AUTH_PG[(PostgreSQL :5434)]
        AUTH_MIG[migrate auth]
    end

    subgraph User Database Container
        USER_PG[(PostgreSQL :5433)]
        USER_MIG[migrate user]
    end

    subgraph Services
        AUTH[Auth Service]
        USER[User Service]
    end

    subgraph Caching
        REDIS[(Redis :6379)]
    end

    subgraph Messaging
        RABBIT[(RabbitMQ :5672)]
    end

    AUTH_MIG -->|on startup| AUTH_PG
    USER_MIG -->|on startup| USER_PG
    AUTH --> AUTH_PG
    AUTH --> REDIS
    AUTH --> RABBIT
    USER --> USER_PG
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
REDIS_URL="redis://localhost:6379"
```
