# Infra Worker

**Go**

Automatically restarts unhealthy services based on health check events.

## Overview

| Property | Value |
|----------|-------|
| **Queue** | `infra-events` |
| **Routing Key** | `hellarch.infra.#` |
| **Backends** | Docker, Kubernetes |
| **Location** | `src/workers/infra/` |

## Architecture

```mermaid
graph TB
    subgraph Health Service
        H[Health Poller]
    end

    subgraph RabbitMQ
        EX[hellarch.events]
        Q[infra-events]
    end

    subgraph Infra Worker
        C[Consumer]
        HN[Handlers]
        O[Orchestrator]
    end

    subgraph Container Runtime
        D[Docker API]
        K[Kubernetes API]
    end

    H -->|service.unhealthy| EX
    H -->|service.recovered| EX
    EX -->|hellarch.infra.#| Q
    Q --> C
    C --> HN
    HN --> O
    O --> D
    O --> K
```

## How It Works

```mermaid
sequenceDiagram
    participant H as Health Service
    participant RMQ as RabbitMQ
    participant I as Infra Worker
    participant D as Docker/K8s

    H->>H: Poll service
    H->>H: Detect unhealthy
    H->>RMQ: Publish service.unhealthy
    RMQ->>I: Deliver event
    I->>I: Check cooldown
    I->>I: Check max restarts
    I->>D: RestartService(name)
    D-->>I: OK
    I->>I: Record restart
```

## Event Types

| Event | Description |
|-------|-------------|
| `hellarch.infra.service.unhealthy.v1` | Service failed health check |
| `hellarch.infra.service.recovered.v1` | Service recovered |

### ServiceUnhealthy Payload

```json
{
  "service_name": "user",
  "status": "unhealthy",
  "consecutive_failures": 3,
  "last_error": "connection refused"
}
```

### ServiceRecovered Payload

```json
{
  "service_name": "user",
  "downtime_seconds": 45
}
```

## Restart Protection

The worker implements several safeguards:

### Cooldown Period

Prevents rapid restarts of the same service:

```go
CooldownSeconds: 60  // Wait 60s between restarts
```

### Max Restarts Window

Limits restarts within a time window:

```go
MaxRestarts: 3           // Max 3 restarts
RestartWindowSecs: 300   // Within 5 minutes
```

### Excluded Services

Some services are never restarted:

- `infra-worker` (self)
- `health` (health service)
- Custom exclusions via `EXCLUDE_SERVICES`

## Orchestrator Backends

### Docker

Uses Docker API to restart containers:

```go
func (d *DockerOrchestrator) RestartService(ctx context.Context, name string) error {
    return d.client.ContainerRestart(ctx, "hellarch-"+name, container.StopOptions{})
}
```

### Kubernetes

Uses Kubernetes API to delete pods (triggers recreation):

```go
func (k *KubernetesOrchestrator) RestartService(ctx context.Context, name string) error {
    return k.client.CoreV1().Pods(k.namespace).DeleteCollection(ctx, 
        metav1.DeleteOptions{}, 
        metav1.ListOptions{LabelSelector: "app=" + name})
}
```

## Environment Variables

See [Workers Configuration](/guide/configuration#workers).

All variables are optional with sensible defaults.

## Docker

```yaml
infra-worker:
  build:
    context: ../../src/workers/infra
    dockerfile: Dockerfile
  environment:
    - RABBITMQ_URL=amqp://hellarch:hellarch@rabbitmq:5672
    - ORCHESTRATOR_BACKEND=docker
    - OTEL_SERVICE_NAME=infra-worker
  volumes:
    - /var/run/docker.sock:/var/run/docker.sock  # Docker API access
```

## Code Structure

```
src/workers/infra/
├── main.go                           # Entry point, consumer setup
├── internal/
│   ├── orchestrator/
│   │   ├── orchestrator.go           # Interface + factory
│   │   ├── docker.go                 # Docker implementation
│   │   └── kubernetes.go             # Kubernetes implementation
│   └── handlers/
│       └── handlers.go               # Event handlers + restart logic
├── go.mod
├── go.sum
├── Dockerfile
└── .air.toml
```

## Logging

```json
{
  "level": "WARN",
  "msg": "restarting unhealthy service",
  "service": "user"
}

{
  "level": "INFO",
  "msg": "service restart initiated",
  "service": "user"
}

{
  "level": "WARN",
  "msg": "service exceeded max restarts in window, skipping",
  "service": "user",
  "max_restarts": 3
}
```
