# Guide

## Project Structure

```
hellArch/
├── src/
│   ├── gateway/           # .NET YARP Gateway
│   ├── services/
│   │   ├── auth/          # Go Auth Service
│   │   ├── user/          # F# User Service
│   │   └── health/        # Gleam Health Service
│   ├── workers/           # Background workers (TBD)
│   ├── schema/
│   │   ├── Schema/        # EF Core contexts + migrations
│   │   ├── Schema.Codegen/# Legacy reflection codegen
│   │   └── MigrationRunner/# Migration runner
│   └── clients/
│       ├── Admin/         # Blazor Admin Console
│       ├── App/           # Vue.js Main App
│       └── Login/         # Blazor Login Portal
├── libs/
│   ├── protos/            # gRPC definitions
│   ├── data_types/        # Entity types (persistence)
│   │   ├── csharp/        # Source of truth
│   │   ├── go/            # Generated
│   │   ├── fsharp/        # Generated
│   │   ├── python/        # Generated
│   │   ├── typescript/    # Generated
│   │   └── Codegen/       # Roslyn codegen
│   ├── cloud_events/      # CloudEvent payloads
│   │   ├── csharp/        # Source of truth
│   │   ├── go/            # Generated
│   │   ├── fsharp/        # Generated
│   │   ├── python/        # Generated
│   │   └── Codegen/       # Roslyn codegen
│   └── design/            # Shared design system
│       ├── tokens.css     # CSS custom properties
│       └── uno.config.ts  # UnoCSS config
├── tests/                 # pytest test suite
│   ├── api/               # API tests
│   ├── integration/       # Integration tests
│   └── fixtures/          # Test fixtures
├── infra/
│   ├── dbs/               # DB containers with migrations
│   ├── sops/              # SOPS/age encryption keys
│   └── docker/            # Docker Compose files
├── scripts/               # Dev scripts
├── Tiltfile               # Tilt orchestration
├── .mise.toml             # Tool versions + tasks
└── .envrc                 # Environment config
```

## Development Prerequisites

Managed via mise (`.mise.toml`):
- .NET 10
- Go 1.24
- Gleam
- Node 22
- Python 3.12

Infrastructure:
- Docker & Docker Compose
- Tilt

## Quick Start

```bash
# 1. Clone and enter directory
cd hellArch

# 2. Allow direnv (loads mise + env vars)
direnv allow

# 3. Run setup script
./scripts/setup-dev.sh

# 4. Start everything
tilt up
```

## Service Ports

| Service | HTTP | gRPC |
|---------|------|------|
| Gateway | 5000 | - |
| Auth | 5001 | 50051 |
| User | 5002 | - |
| Admin Console | 5003 | - |
| Health | 5004 | - |
| PostgreSQL (Auth) | 5434 | - |
| PostgreSQL (User) | 5433 | - |
| Redis | 6379 | - |
| RabbitMQ | 5672 | 15672 (UI) |
| Jaeger | 16686 | - |
