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

## API Endpoints

### Upload

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/files/upload` | Direct file upload |
| POST | `/api/files/presign/upload` | Get presigned upload URL |
| POST | `/api/files/{id}/confirm` | Confirm presigned upload |

### Download

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/files/{id}` | Download file |
| GET | `/api/files/{id}/presign` | Get presigned download URL |

### Metadata

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/files/{id}/meta` | Get file metadata |
| GET | `/api/files` | List files (paginated) |
| DELETE | `/api/files/{id}` | Delete file |

### Variants

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/files/{id}/variants` | List file variants |
| GET | `/api/files/{id}/variants/{type}` | Download variant |
| POST | `/api/files/{id}/variants` | Create variant record |

### Health

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |

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

### stored_files

```sql
CREATE TABLE stored_files (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID NOT NULL,
    filename TEXT NOT NULL,
    mime_type TEXT NOT NULL,
    size_bytes BIGINT NOT NULL,
    object_key TEXT NOT NULL UNIQUE,
    status TEXT NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### file_variants

```sql
CREATE TABLE file_variants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_id UUID NOT NULL REFERENCES stored_files(id) ON DELETE CASCADE,
    variant_type TEXT NOT NULL,
    object_key TEXT NOT NULL,
    mime_type TEXT NOT NULL,
    size_bytes BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(file_id, variant_type)
);
```

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
| `MINIO_SECRET_KEY` | Yes | - | MinIO secret key |
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
