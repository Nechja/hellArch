# Files Service

**Go**

File storage and management service with MinIO object storage backend.

## Overview

| Property | Value |
|----------|-------|
| **Port** | 5006 |
| **Database** | `files_db` |
| **Storage** | MinIO |
| **Location** | `src/services/files/` |

## Architecture

```mermaid
graph TB
    subgraph Files Service :5006
        API[HTTP API]
        ST[(Store)]
        MINIO[MinIO Client]
        PUB[RabbitMQ Publisher]
    end

    subgraph Storage
        DB[(files_db)]
        OBJ[(MinIO)]
    end

    subgraph RabbitMQ
        EX[hellarch.events]
    end

    subgraph Consumers
        IMAGES[Images Worker]
        ADMIN[Admin Console]
    end

    ADMIN -->|REST| API
    API --> ST
    ST --> DB
    API --> MINIO
    MINIO --> OBJ
    API --> PUB
    PUB --> EX
    EX --> IMAGES
```

## API Reference

See [Files API](/api/files) for complete endpoint documentation.

::: tip Direct Routing
Unlike other services, the Files service receives requests with the full `/api/files/` prefix. The Gateway forwards requests without path transformation.
:::

**Key endpoints:**
- `POST /api/files/upload` - Direct file upload
- `POST /api/files/presign/upload` - Get presigned upload URL
- `GET /api/files/{id}` - Download file
- `GET /api/files/{id}/meta` - Get file metadata
- `GET /api/files/{id}/variants` - List/download variants

## How It Works

```mermaid
sequenceDiagram
    participant C as Client
    participant F as Files Service
    participant M as MinIO
    participant DB as PostgreSQL
    participant RMQ as RabbitMQ
    participant I as Images Worker

    C->>F: POST /api/files/upload
    F->>M: Upload object
    M-->>F: Object key
    F->>DB: Insert stored_files record
    F->>RMQ: Publish file.uploaded
    F-->>C: {id, object_key}

    RMQ->>I: file.uploaded event
    I->>F: GET /api/files/{id}/meta
    I->>M: Download original
    I->>I: Generate thumbnail
    I->>M: Upload thumbnail
    I->>F: POST /api/files/{id}/variants
    I->>RMQ: Publish file.processed
```

## Database Schema

See [Files Database](/guide/database#files-database) for full schema.

**Tables:** `stored_files`, `file_variants`

## CloudEvents

The Files Service publishes these events:

| Event Type | Trigger |
|------------|---------|
| `hellarch.files.file.uploaded.v1` | File uploaded successfully |
| `hellarch.files.file.deleted.v1` | File deleted |

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DATABASE_URL` | Yes | - | PostgreSQL connection string |
| `RABBITMQ_URL` | No | `amqp://guest:guest@localhost:5672` | RabbitMQ connection |
| `MINIO_ENDPOINT` | No | `localhost:9000` | MinIO endpoint |
| `MINIO_ACCESS_KEY` | No | `hellarch` | MinIO access key |
| `MINIO_SECRET_KEY` | No | `hellarch` | MinIO secret key |
| `MINIO_BUCKET` | No | `hellarch-files` | Storage bucket |
| `MINIO_USE_SSL` | No | `false` | Use HTTPS for MinIO |
| `MAX_UPLOAD_SIZE` | No | `1073741824` | Max upload size (1GB) |
| `HTTP_ADDRESS` | No | `:5006` | HTTP listen address |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | `localhost:4318` | OTLP endpoint |

## Code Structure

```
src/services/files/
├── cmd/server/main.go           # Entry point
├── internal/
│   ├── config/config.go         # Environment config
│   ├── handler/
│   │   ├── http.go              # Route registration
│   │   ├── upload.go            # Upload handlers
│   │   ├── download.go          # Download handlers
│   │   └── metadata.go          # Metadata handlers
│   ├── service/
│   │   ├── file.go              # File business logic
│   │   ├── upload.go            # Upload business logic
│   │   └── variant.go           # Variant business logic
│   ├── storage/
│   │   └── minio.go             # MinIO client
│   └── store/
│       ├── postgres.go          # DB connection
│       ├── files.go             # File queries
│       └── variants.go          # Variant queries
├── go.mod
├── go.sum
└── Dockerfile
```

## Docker

```yaml
files:
  build:
    context: ../../src/services/files
    dockerfile: Dockerfile
  ports:
    - "5006:5006"
  environment:
    - DATABASE_URL=postgres://hellarch:hellarch@files-db:5432/files_db
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - MINIO_ENDPOINT=minio:9000
    - MINIO_ACCESS_KEY=hellarch
    - MINIO_SECRET_KEY=hellarch
    - OTEL_SERVICE_NAME=files
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```
