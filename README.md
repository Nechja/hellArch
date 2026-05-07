![hellarch](hellarch.png)

A polyglot microservices architecture built as a learning project. Intentionally complex and messy to explore multiple languages, patterns, and infrastructure approaches.

## Architecture Overview

HellArch is a distributed system with:

- API Gateway (C#) - Central routing and HTTP/gRPC entry point
- Microservices - Written in Go, C#, and F#
- Event streaming with RabbitMQ and CloudEvents
- OAuth authentication (Discord, GitHub, Google, Microsoft, Apple, Twitch)
- Real-time WebSocket workers
- Multiple databases per service
- Object storage via MinIO
- Distributed tracing with Jaeger

## Services

### Backend

- **gateway** (C#) - HTTP/gRPC entry point
- **auth** (Go) - OAuth and JWT token management
- **user** (F#) - User profiles and account data
- **identity** (F#) - Optional self-hosted authentication
- **events** (Go) - Event storage and streaming
- **files** (Go) - File uploads and MinIO integration
- **health** (Gleam) - Service health monitoring

### Workers

- **audit** (Go) - Event logging
- **webhooks** (Go) - Webhook delivery
- **realtime** (C#) - WebSocket real-time updates
- **infra** (Go) - Infrastructure tasks
- **dlq** (C#) - Dead-letter queue handling
- **images** (Go) - Image processing

### Clients

- **Admin** (C# Blazor) - Admin dashboard
- **App** (Vue.js) - Main application
- **Identity** (Vue.js) - Self-hosted auth UI

## Databases

- auth-db - Authentication data
- user-db - User profiles
- identity-db - Self-hosted auth (optional)
- events-db - Event logs
- files-db - File metadata

Migrations run automatically on startup.

## Shared Libraries

Located in `/libs`:

- cloud_events - CloudEvents implementations (Go, C#, F#)
- data_types - Shared models with code generation
- protos - Protocol Buffer definitions
- csharp - .NET utilities
- fsharp - F# utilities
- go - Go shared packages
- components - Blazor component library
- design - Design tokens

## Getting Started

Quit now.

But if you must...

Tilt will spin up all infrastructure, build services, and watch for changes. Access the Tilt UI at http://localhost:10350.

### Services Once Running

- Gateway: http://localhost:5000
- Admin: http://localhost:5003
- MinIO: http://localhost:9001
- Jaeger: http://localhost:16686
- Tilt UI: http://localhost:10350

## Security & Secrets

Uses SOPS (Secrets Operations) for encryption:

- Secrets stored in `infra/sops/`
- OAuth credentials, database passwords, JWT keys are encrypted
- Configuration in `.sops.yaml`
- JWT keys auto-generated in development

## Development

### Code Generation

Generate type definitions:
```bash
cd libs/data_types
mise exec -- dotnet run --project Codegen/Codegen.csproj -- csharp .
```

Generate Protocol Buffers:
```bash
./scripts/generate-protos.sh
```

### Database Migrations

Migrations run automatically. Manual run:
```bash
docker-compose exec auth-db dotnet run --project MigrationRunner/MigrationRunner.csproj
```

### Testing

```bash
docker-compose exec testing python -m pytest
```

## Infrastructure Stack

- RabbitMQ - Message queue with CloudEvents
- Redis - Caching
- MinIO - S3-compatible object storage
- Jaeger - Distributed tracing
- PostgreSQL - Multiple databases
- Docker Compose - Orchestration

## Project Structure

```
src/
  gateway/       - C# API Gateway
  services/      - Microservices (Go, F#)
  workers/       - Background workers
  clients/       - Frontend apps
  schema/        - Schemas and migrations
libs/            - Shared language-specific libraries
infra/
  docker/        - Docker Compose files
  dbs/           - Database Dockerfiles
  config/        - Config templates
  sops/          - Encrypted secrets
scripts/         - Development scripts
Tiltfile         - Tilt configuration
```

## Configuration

Environment variables in `.env`:

- OAuth providers: DISCORD_CLIENT_ID, GITHUB_CLIENT_ID, GOOGLE_CLIENT_ID, etc.
- Databases: AUTH_DATABASE_URL, USER_DATABASE_URL
- Redis: REDIS_URL
- RabbitMQ: RABBITMQ_URL, RABBITMQ_USER
- JWT: JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE
- Services: GRPC_ADDRESS, HTTP_ADDRESS

See `.env.example` for all variables.

## Code Quality

- C#: Nullable enabled, strict analyzers (WarningLevel 9999)
- F#: Strict warnings
- Go: Standard linting
- Python: Ruff (see ruff.toml)

## Event Model

Services publish events to RabbitMQ following CloudEvents spec. Workers subscribe to events they care about. Examples: user creation, file uploads, audit logging.

## Notes

This project deliberately combines many languages and patterns. It's messy by design a playground for learning infrastructure, not a production template, unless you're into that then go right ahead. Good for understanding polyglot systems, event-driven architecture, and distributed tracing.

## Repository

https://github.com/Nechja/hellArch
