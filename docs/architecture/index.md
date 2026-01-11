# Architecture Overview

HellArch is a polyglot microservices system for authentication and user management.

## System Diagram

```mermaid
graph TB
    subgraph Clients
        ADMIN[Admin Console<br/>:5003]
        IDENTITY_UI[Identity Client<br/>Vue SPA]
    end

    subgraph Gateway["Gateway :5000"]
        YARP[YARP Reverse Proxy]
        MW[Auth Middleware]
    end

    subgraph Services
        AUTH[Auth Service<br/>Go :5001/:50051]
        USER[User Service<br/>F# :5002]
        HEALTH[Health Service<br/>Gleam :5004]
        EVENTS[Events Service<br/>Go :5005]
        FILES[Files Service<br/>Go :5006]
        IDENTITY[Identity Service<br/>F# :5010]
    end

    subgraph Infrastructure
        PG_AUTH[(PostgreSQL<br/>Auth DB :5434)]
        PG_USER[(PostgreSQL<br/>User DB :5433)]
        PG_EVENTS[(PostgreSQL<br/>Events DB)]
        PG_FILES[(PostgreSQL<br/>Files DB)]
        PG_IDENTITY[(PostgreSQL<br/>Identity DB :5437)]
        REDIS[(Redis :6379)]
        RABBIT[(RabbitMQ :5672)]
        MINIO[(MinIO)]
    end

    ADMIN --> YARP
    ADMIN --> REDIS
    IDENTITY_UI --> YARP
    YARP --> MW
    MW -->|/api/auth/*| AUTH
    MW -->|/api/user/*| USER
    MW -->|/api/events/*| EVENTS
    MW -->|/api/files/*| FILES
    MW -->|/api/identity/*| IDENTITY

    AUTH --> PG_AUTH
    AUTH --> REDIS
    AUTH --> IDENTITY
    USER --> PG_USER
    EVENTS --> PG_EVENTS
    FILES --> PG_FILES
    FILES --> MINIO
    IDENTITY --> PG_IDENTITY
    AUTH -.->|gRPC| USER
    AUTH -->|CloudEvents| RABBIT
    EVENTS -->|CloudEvents| RABBIT
    FILES -->|CloudEvents| RABBIT
    RABBIT --> USER
    HEALTH -->|HTTP| AUTH
    HEALTH -->|HTTP| USER
    HEALTH -->|HTTP| EVENTS
    HEALTH -->|HTTP| FILES
    HEALTH -->|HTTP| IDENTITY
    HEALTH -->|TCP| REDIS
    ADMIN --> HEALTH
```

## Service Responsibilities

| Service | Language | Port | Role |
|---------|----------|------|------|
| Gateway | C# | 5000 | Request routing, cookie-to-JWT transform |
| Auth | Go | 5001 (HTTP), 50051 (gRPC) | OAuth flows, JWT tokens, sessions |
| User | F# | 5002 | Profiles, preferences, linked accounts |
| Admin Console | C# Blazor | 5003 | User management, Redis browser, health dashboard |
| Health | Gleam | 5004 | Aggregates health status from all services |
| Events | Go | 5005 | Audit logs, webhooks, dead letters, worker monitoring |
| Files | Go | 5006 | File storage with MinIO, variants |
| Identity | F# | 5010 | OAuth 2.0 authorization server, user registration |

## Technology Stack

```mermaid
graph LR
    subgraph Runtime
        GO[Go 1.24]
        GLEAM[Gleam 1.14]
        DOTNET[.NET 10]
        PY[Python 3.12]
    end

    subgraph Data
        PG[PostgreSQL 17]
        RD[Redis 7]
        RMQ[RabbitMQ 3]
    end

    subgraph Protocols
        GRPC[gRPC/Protobuf]
        HTTP[HTTP/REST]
        CE[CloudEvents]
    end

    subgraph DevTools
        TILT[Tilt]
        MISE[mise]
        DOCKER[Docker Compose]
    end
```

## Type System

Single source of truth for data types with multi-language generation:

```mermaid
graph TB
    subgraph Source
        CS[C# Classes<br/>libs/data_types/csharp]
    end

    subgraph Generated
        GO[Go structs]
        FS[F# records]
        PY[Python models]
        TS[TypeScript interfaces]
    end

    subgraph Consumers
        EF[EF Core Migrations]
        AUTH[Auth Service]
        USER[User Service]
        TESTS[pytest Tests]
    end

    CS --> GO
    CS --> FS
    CS --> PY
    CS --> TS
    CS --> EF
    GO --> AUTH
    FS --> USER
    PY --> TESTS
```

## Communication Patterns

```mermaid
graph LR
    subgraph Synchronous
        HTTP[HTTP/REST<br/>External API]
        GRPC[gRPC<br/>Service-to-service]
    end

    subgraph Asynchronous
        CE[CloudEvents<br/>via RabbitMQ]
    end

    HTTP -->|Clients| GW[Gateway]
    GRPC -->|Internal| SVC[Services]
    CE -->|Events| QUEUE[Message Queue]
```

| Pattern | Use Case | Example |
|---------|----------|---------|
| HTTP/REST | External API calls | Client → Gateway → Service |
| gRPC | Low-latency service calls | Gateway → Auth (token validation) |
| CloudEvents | Async notifications | Auth → User (user.created) |
