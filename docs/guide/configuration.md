# Configuration Reference

All services are configured via environment variables. This page consolidates all configuration options.

## Common Variables

These variables are used across multiple services:

| Variable | Description |
|----------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry collector endpoint |
| `OTEL_SERVICE_NAME` | Service name for tracing |
| `LOG_LEVEL` | Logging level (`debug`, `info`, `warn`, `error`) |

## Gateway

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://+:5000` | Listen URL |
| `Auth__GrpcAddress` | `http://auth:50051` | Auth service gRPC address |

## Auth Service

| Variable | Default | Description |
|----------|---------|-------------|
| `HTTP_ADDRESS` | `:5001` | HTTP listen address |
| `GRPC_ADDRESS` | `:50051` | gRPC listen address |
| `AUTH_DATABASE_URL` | - | PostgreSQL connection |
| `REDIS_URL` | `redis://localhost:6379` | Redis connection |
| `RABBITMQ_URL` | `amqp://hellarch:hellarch@localhost:5672` | RabbitMQ connection |
| `JWT_PRIVATE_KEY_PATH` | `./infra/keys/dev-private.pem` | RSA private key |
| `JWT_PUBLIC_KEY_PATH` | `./infra/keys/dev-public.pem` | RSA public key |
| `ALLOWED_REDIRECT_URIS` | `http://localhost:5000,http://localhost:5003` | Valid OAuth redirect URIs |
| `SECURE_COOKIES` | `false` | Use secure cookies (HTTPS) |
| `MAX_SESSIONS_PER_USER` | `5` | Maximum concurrent sessions |
| `SESSION_DURATION_HOURS` | `24` | Session lifetime |

### OAuth Providers

| Provider | Variables |
|----------|-----------|
| Discord | `DISCORD_CLIENT_ID`, `DISCORD_CLIENT_SECRET`, `DISCORD_REDIRECT_URI` |
| Google | `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_REDIRECT_URI` |
| GitHub | `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET`, `GITHUB_REDIRECT_URI` |
| Microsoft | `MICROSOFT_CLIENT_ID`, `MICROSOFT_CLIENT_SECRET`, `MICROSOFT_REDIRECT_URI` |
| Apple | `APPLE_CLIENT_ID`, `APPLE_TEAM_ID`, `APPLE_KEY_ID`, `APPLE_PRIVATE_KEY_PATH`, `APPLE_REDIRECT_URI` |
| Twitch | `TWITCH_CLIENT_ID`, `TWITCH_CLIENT_SECRET`, `TWITCH_REDIRECT_URI` |
| HellArch | `HELLARCH_CLIENT_ID`, `HELLARCH_CLIENT_SECRET`, `HELLARCH_REDIRECT_URI`, `HELLARCH_IDENTITY_URL`, `HELLARCH_IDENTITY_PUBLIC_URL` |

## User Service

| Variable | Default | Description |
|----------|---------|-------------|
| `HTTP_PORT` | `5002` | HTTP listen port |
| `USER_DATABASE_URL` | - | PostgreSQL connection |
| `RABBITMQ_URL` | `amqp://guest:guest@localhost:5672` | RabbitMQ connection |
| `AUTH_GRPC_URL` | `http://localhost:50051` | Auth service gRPC address |

## Events Service

| Variable | Default | Description |
|----------|---------|-------------|
| `HTTP_ADDRESS` | `:5005` | HTTP listen address |
| `DATABASE_URL` | - | PostgreSQL connection |
| `RABBITMQ_URL` | - | RabbitMQ AMQP URL |
| `RABBITMQ_MANAGEMENT_URL` | - | RabbitMQ Management API URL |

## Files Service

| Variable | Default | Description |
|----------|---------|-------------|
| `HTTP_ADDRESS` | `:5006` | HTTP listen address |
| `DATABASE_URL` | - | PostgreSQL connection |
| `RABBITMQ_URL` | `amqp://guest:guest@localhost:5672` | RabbitMQ connection |
| `MINIO_ENDPOINT` | `localhost:9000` | MinIO endpoint |
| `MINIO_ACCESS_KEY` | `hellarch` | MinIO access key |
| `MINIO_SECRET_KEY` | `hellarch` | MinIO secret key |
| `MINIO_BUCKET` | `hellarch-files` | Storage bucket |
| `MINIO_USE_SSL` | `false` | Use HTTPS for MinIO |
| `MAX_UPLOAD_SIZE` | `1073741824` | Max upload size (1GB) |

## Identity Service

| Variable | Default | Description |
|----------|---------|-------------|
| `HTTP_PORT` | `5010` | HTTP listen port |
| `IDENTITY_DATABASE_URL` | - | PostgreSQL connection |
| `IDENTITY_CLIENT_URL` | `http://localhost:5000/identity` | Frontend login page URL |
| `IDENTITY_ALLOWED_ORIGINS` | `http://localhost:5000,http://localhost:3000` | CORS allowed origins |
| `IDENTITY_SEED_CLIENT_ID` | - | OAuth client ID to seed |
| `IDENTITY_SEED_CLIENT_SECRET` | - | OAuth client secret to seed |
| `IDENTITY_SEED_REDIRECT_URIS` | - | Comma-separated redirect URIs |

## Health Service

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `5004` | HTTP listen port |
| `POLL_INTERVAL_MS` | `10000` | Health check interval |
| `HEALTH_CONFIG_PATH` | `/app/config/health-services.json` | Service configuration |

## Admin Console

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `5003` | HTTP listen port |
| `GATEWAY_URL` | `http://localhost:5000` | Gateway URL for API calls |
| `REDIS_URL` | `localhost:6379` | Redis connection |
| `RABBITMQ_MANAGEMENT_URL` | `http://localhost:15672` | RabbitMQ Management API |
| `RABBITMQ_USER` | `hellarch` | RabbitMQ username |
| `RABBITMQ_PASSWORD` | `hellarch` | RabbitMQ password |

## Workers

All workers share common configuration:

| Variable | Default | Description |
|----------|---------|-------------|
| `RABBITMQ_URL` | `amqp://guest:guest@localhost:5672` | RabbitMQ connection |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `localhost:4318` | OpenTelemetry collector |

**Audit & Webhooks Workers:**

| Variable | Default | Description |
|----------|---------|-------------|
| `DATABASE_URL` | - | PostgreSQL connection (events_db) |

**Realtime Worker:**

| Variable | Default | Description |
|----------|---------|-------------|
| `JWT_SECRET` | - | JWT signing key for WebSocket auth |
| `JWT_ISSUER` | `hellarch` | Expected JWT issuer |
| `JWT_AUDIENCE` | `hellarch` | Expected JWT audience |
| `CORS_ORIGINS` | `localhost:5000,5010` | Allowed CORS origins |

**DLQ Worker:**

Uses common configuration only (`RABBITMQ_URL`, `OTEL_EXPORTER_OTLP_ENDPOINT`).

**Images Worker:**

| Variable | Default | Description |
|----------|---------|-------------|
| `MINIO_ENDPOINT` | `localhost:9000` | MinIO endpoint |
| `MINIO_ACCESS_KEY` | `hellarch` | MinIO access key |
| `MINIO_SECRET_KEY` | - | MinIO secret key |
| `FILES_SERVICE_URL` | `http://localhost:5006` | Files service URL |

**Infra Worker:**

| Variable | Default | Description |
|----------|---------|-------------|
| `ORCHESTRATOR_BACKEND` | `docker` | `docker` or `kubernetes` |
| `RESTART_COOLDOWN_SECONDS` | `60` | Cooldown between restarts |
| `MAX_RESTARTS` | `3` | Max restarts in time window |
| `RESTART_WINDOW_SECONDS` | `300` | Window for max restart count |
| `EXCLUDE_SERVICES` | - | Comma-separated service exclusions |

## Database Connection Strings

| Service | Variable | Default Port |
|---------|----------|--------------|
| Auth | `AUTH_DATABASE_URL` | 5434 |
| User | `USER_DATABASE_URL` | 5433 |
| Events | `DATABASE_URL` | 5435 |
| Files | `DATABASE_URL` | 5436 |
| Identity | `IDENTITY_DATABASE_URL` | 5437 |

Format: `postgres://hellarch:hellarch@localhost:{port}/{db}_db`

## Infrastructure

| Service | Port | Management |
|---------|------|------------|
| Redis | 6379 | - |
| RabbitMQ | 5672 | 15672 |
| MinIO | 9000 | 9001 |
| Jaeger | - | 16686 |
