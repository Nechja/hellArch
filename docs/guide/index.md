# Guide

## Project Structure

```
hellArch/
├── src/
│   ├── gateway/           # .NET YARP Gateway
│   ├── services/
│   │   ├── auth/          # Go Auth Service
│   │   ├── events/        # Go Events Service
│   │   ├── files/         # Go Files Service
│   │   ├── health/        # Gleam Health Service
│   │   ├── identity/      # F# Identity Service
│   │   ├── testing/       # Python Testing Service
│   │   └── user/          # F# User Service
│   ├── workers/           # Background workers
│   │   ├── audit/         # Go Audit Worker
│   │   ├── images/        # Go Image Processing Worker
│   │   └── webhooks/      # Go Webhooks Worker
│   ├── schema/
│   │   ├── Schema/        # EF Core contexts + migrations
│   │   ├── Schema.Codegen/# Legacy reflection codegen
│   │   └── MigrationRunner/# Migration runner
│   └── clients/
│       ├── Admin/         # Blazor Admin Console (MudBlazor)
│       └── App/           # Vue.js Main App (Vuetify)
├── libs/
│   ├── protos/            # gRPC definitions
│   ├── data_types/        # Entity types (persistence)
│   ├── cloud_events/      # CloudEvent payloads
│   ├── go/                # Go shared libraries
│   ├── csharp/            # C# shared libraries
│   ├── fsharp/            # F# shared libraries
│   └── design/            # Shared design system
├── docs/                  # VitePress documentation
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
| Events | 5005 | - |
| Files | 5006 | - |
| Testing | 5007 | - |
| Identity | 5010 | - |
| PostgreSQL (Auth) | 5434 | - |
| PostgreSQL (User) | 5433 | - |
| PostgreSQL (Events) | 5435 | - |
| PostgreSQL (Files) | 5436 | - |
| PostgreSQL (Identity) | 5437 | - |
| Redis | 6379 | - |
| RabbitMQ | 5672 | 15672 (UI) |
| Jaeger | 16686 | - |
