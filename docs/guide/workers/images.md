# Images Worker

**Go**

Processes uploaded images to generate thumbnails and other variants.

## Overview

| Property | Value |
|----------|-------|
| **Queue** | `images-worker` |
| **Routing Key** | `hellarch.files.file.uploaded.v1` |
| **Storage** | MinIO |
| **Location** | `src/workers/images/` |

## Architecture

```mermaid
graph TB
    subgraph RabbitMQ
        EX[hellarch.events]
        Q[images-worker]
    end

    subgraph Images Worker
        C[Consumer]
        P[Processor]
    end

    subgraph Files Service
        API[Files API :5006]
    end

    subgraph Storage
        MINIO[(MinIO)]
    end

    EX -->|file.uploaded| Q
    Q --> C
    C --> P
    P -->|Get metadata| API
    P -->|Download original| MINIO
    P -->|Upload thumbnail| MINIO
    P -->|Create variant| API
    P -->|file.processed| EX
```

## How It Works

```mermaid
sequenceDiagram
    participant RMQ as RabbitMQ
    participant C as Consumer
    participant P as Processor
    participant F as Files API
    participant M as MinIO

    RMQ->>C: file.uploaded event
    C->>C: Check if image/*

    alt Not an image
        C->>RMQ: Ack (skip)
    else Is an image
        C->>P: Process(event)
        P->>F: GET /api/files/{id}/meta
        F-->>P: {object_key}
        P->>M: Download original
        M-->>P: Image data
        P->>P: Generate thumbnail
        P->>M: Upload thumbnail
        P->>F: POST /api/files/{id}/variants
        P->>RMQ: Publish file.processed
        C->>RMQ: Ack message
    end
```

## Processing Pipeline

1. **Event Received**: Consumes `file.uploaded` events from RabbitMQ
2. **MIME Check**: Skips non-image files (only processes `image/*`)
3. **Fetch Metadata**: Gets object key from Files Service API
4. **Download Original**: Retrieves image from MinIO
5. **Generate Thumbnail**: Creates 256x256 JPEG thumbnail
6. **Upload Variant**: Stores thumbnail in MinIO with `_thumb.jpg` suffix
7. **Create Record**: Registers variant with Files Service API
8. **Publish Result**: Emits `file.processed` event

## Configuration

| Constant | Default | Description |
|----------|---------|-------------|
| `ThumbnailSize` | 256 | Thumbnail dimension (px) |
| `ThumbnailFormat` | JPEG | Output format |
| `QueueName` | `images-worker` | RabbitMQ queue |

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `RABBITMQ_URL` | Yes | - | RabbitMQ connection |
| `MINIO_ENDPOINT` | Yes | - | MinIO endpoint |
| `MINIO_ACCESS_KEY` | Yes | - | MinIO access key |
| `MINIO_SECRET_KEY` | Yes | - | MinIO secret key |
| `MINIO_BUCKET` | No | `hellarch-files` | Storage bucket |
| `MINIO_USE_SSL` | No | `false` | Use HTTPS for MinIO |
| `FILES_API_URL` | No | `http://files:5006` | Files Service URL |
| `THUMBNAIL_SIZE` | No | `256` | Thumbnail size (px) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | - | OTLP endpoint |

## CloudEvents

### Consumed

| Event Type | Source |
|------------|--------|
| `hellarch.files.file.uploaded.v1` | Files Service |

### Published

| Event Type | Trigger |
|------------|---------|
| `hellarch.files.file.processed.v1` | Thumbnail generated (success or failure) |

## File Processed Event

```json
{
  "specversion": "1.0",
  "type": "hellarch.files.file.processed.v1",
  "source": "images-worker",
  "id": "evt_123",
  "time": "2024-01-15T10:30:00Z",
  "data": {
    "file_id": "uuid",
    "variant_type": "thumbnail",
    "status": "success",
    "error": null,
    "processed_at": "2024-01-15T10:30:00Z"
  }
}
```

## Code Structure

```
src/workers/images/
├── cmd/main.go                  # Entry point
├── internal/
│   ├── config/config.go         # Environment config
│   ├── consumer/consumer.go     # RabbitMQ consumer
│   ├── processor/
│   │   ├── processor.go         # Main processing logic
│   │   └── thumbnails.go        # Image resizing
│   └── storage/
│       └── minio.go             # MinIO client
├── go.mod
├── go.sum
└── Dockerfile
```

## OpenTelemetry

Spans created:

- `images.process_event` - Message processing
- `images.generate_thumbnail` - Thumbnail generation
- `images.upload_variant` - Upload to MinIO

Span attributes:
- `file.id`: File UUID
- `file.mime_type`: Original MIME type
- `variant.type`: Variant type (thumbnail)
- `processing.status`: success/failed

## Docker

```yaml
images-worker:
  build:
    context: ../../src/workers/images
    dockerfile: Dockerfile
  environment:
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - MINIO_ENDPOINT=minio:9000
    - MINIO_ACCESS_KEY=hellarch
    - MINIO_SECRET_KEY=hellarch
    - FILES_API_URL=http://files:5006
    - OTEL_SERVICE_NAME=images-worker
    - OTEL_EXPORTER_OTLP_ENDPOINT=jaeger:4318
```

## Supported Formats

The worker processes any file with an `image/*` MIME type, including:

- JPEG (`image/jpeg`)
- PNG (`image/png`)
- GIF (`image/gif`)
- WebP (`image/webp`)

All thumbnails are output as JPEG for consistency and size optimization.

## Error Handling

If processing fails, the worker:

1. Logs the error with file ID and details
2. Publishes a `file.processed` event with `status: "failed"` and error message
3. Acknowledges the message (no retry to avoid infinite loops)

Failed processing can be monitored via:
- Audit logs
- CloudEvents dashboard
- OpenTelemetry traces
