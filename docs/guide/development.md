# Development Workflow

Local development uses Tilt for orchestration, mise for tool versions, and direnv for environment setup.

## Toolchain

```mermaid
graph LR
    subgraph Tools
        MISE[mise]
        DIRENV[direnv]
        TILT[Tilt]
        DOCKER[Docker]
    end

    MISE -->|manages| VERSIONS[Tool Versions<br/>Go, .NET, Node, Gleam]
    DIRENV -->|loads| ENV[Environment Variables]
    DIRENV -->|activates| MISE
    TILT -->|orchestrates| SERVICES[Services]
    DOCKER -->|runs| INFRA[Infrastructure]
```

## Prerequisites

Install these tools:

| Tool | Purpose |
|------|---------|
| [mise](https://mise.jdx.dev/) | Runtime version manager |
| [direnv](https://direnv.net/) | Environment loader |
| [Tilt](https://tilt.dev/) | Dev orchestration |
| [Docker](https://docker.com/) | Containers |

## Setup

```bash
# Clone repo
git clone https://github.com/Nechja/hellArch
cd hellArch

# Allow direnv (loads mise + env vars)
direnv allow

# Verify tools
mise doctor
```

## Tool Versions

Defined in `.mise.toml`:

```toml
[tools]
dotnet = "10.0"
go = "1.24"
gleam = "latest"
node = "22"
python = "3.12"
uv = "latest"
sops = "latest"
age = "latest"
golangci-lint = "latest"
ruff = "latest"
```

## Environment

Defined in `.envrc`:

```mermaid
graph TB
    subgraph Environment
        MISE_ACTIVATE[mise activate]
        DB_URLS[Database URLs]
        REDIS_URL[Redis URL]
        RABBIT_URL[RabbitMQ URL]
        JWT_KEYS[JWT Key Paths]
        OAUTH[OAuth Credentials]
    end

    .envrc --> MISE_ACTIVATE
    .envrc --> DB_URLS
    .envrc --> REDIS_URL
    .envrc --> RABBIT_URL
    .envrc --> JWT_KEYS
    .envrc --> OAUTH
```

Key variables:
- `AUTH_DATABASE_URL` - Auth PostgreSQL (port 5434)
- `USER_DATABASE_URL` - User PostgreSQL (port 5433)
- `REDIS_URL` - Redis connection
- `JWT_PRIVATE_KEY_PATH` / `JWT_PUBLIC_KEY_PATH` - RSA keys

## Running Services

### Start Everything

```bash
tilt up
```

Opens Tilt UI at http://localhost:10350

### What Tilt Runs

```mermaid
graph TB
    subgraph Tilt
        INFRA[docker-compose.infra.yml]
        GATEWAY[Gateway :5000]
        AUTH[Auth Service :5001]
        USER[User Service :5002]
    end

    subgraph Infrastructure
        PG_AUTH[(PostgreSQL :5434)]
        PG_USER[(PostgreSQL :5433)]
        REDIS[(Redis :6379)]
        RABBIT[(RabbitMQ :5672)]
    end

    INFRA --> PG_AUTH
    INFRA --> PG_USER
    INFRA --> REDIS
    INFRA --> RABBIT

    GATEWAY --> AUTH
    GATEWAY --> USER
    AUTH --> PG_AUTH
    AUTH --> REDIS
    USER --> PG_USER
```

### Service Ports

| Service | Port | URL |
|---------|------|-----|
| Tilt UI | 10350 | http://localhost:10350 |
| Gateway | 5000 | http://localhost:5000 |
| Auth | 5001 | http://localhost:5001 |
| Auth gRPC | 50051 | - |
| User | 5002 | http://localhost:5002 |
| Admin Console | 5003 | http://localhost:5003 |
| Health | 5004 | http://localhost:5004 |
| PostgreSQL (Auth) | 5434 | - |
| PostgreSQL (User) | 5433 | - |
| Redis | 6379 | - |
| RabbitMQ | 5672 | - |
| RabbitMQ UI | 15672 | http://localhost:15672 |
| Jaeger | 16686 | http://localhost:16686 |

## Live Reload

Tilt watches for file changes and rebuilds automatically:

- **Go** - Rebuilds auth service
- **Gleam** - Rebuilds health service
- **.NET** - Rebuilds gateway and user service
- **Proto** - Regenerates gRPC code

## Stopping

```bash
# Stop services (keep containers)
tilt down

# Stop and remove infrastructure
docker compose -f infra/docker/docker-compose.infra.yml down
```

## Mise Tasks

Common tasks defined in `.mise.toml`:

```bash
# Proto generation
mise run proto:all      # Generate all proto types
mise run proto:go       # Generate Go types
mise run proto:fsharp   # Generate F# types

# Codegen
mise run codegen:all      # Generate all types (entities + events)
mise run codegen:entities # Generate entity types from C# models
mise run codegen:events   # Generate CloudEvents types from C# models

# Design system
mise run design:install # Install npm dependencies
mise run design:build   # Generate utility CSS
mise run design:copy    # Copy to Gateway

# Client builds
mise run app:build      # Build Vue app
mise run app:copy       # Copy to Gateway
mise run login:build    # Build Login Blazor WASM
mise run login:copy     # Copy to Gateway
mise run clients:all    # Build and deploy all clients

# Testing
mise run test:all       # Run all tests
mise run test:api       # Run API tests
mise run test:integration # Run integration tests
mise run test:parallel  # Run tests in parallel
```
