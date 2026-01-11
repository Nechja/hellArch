# -*- mode: Python -*-

# Load extensions
load('ext://dotenv', 'dotenv')

# Load environment from .env
dotenv()

# ============================================================================
# Infrastructure (network-only, no exposed ports)
# ============================================================================

docker_compose('./infra/docker/docker-compose.infra.yml')

# Build DB images with migration runner - rebuilds when migrations change
docker_build(
    'hellarch-auth-db',
    '.',
    dockerfile='infra/dbs/auth-db/Dockerfile',
    only=[
        'src/schema/Schema/',
        'src/schema/MigrationRunner/',
        'libs/data_types/csharp/',
        'infra/dbs/auth-db/',
    ],
    ignore=['**/bin', '**/obj'],
)

docker_build(
    'hellarch-user-db',
    '.',
    dockerfile='infra/dbs/user-db/Dockerfile',
    only=[
        'src/schema/Schema/',
        'src/schema/MigrationRunner/',
        'libs/data_types/csharp/',
        'infra/dbs/user-db/',
    ],
    ignore=['**/bin', '**/obj'],
)

docker_build(
    'hellarch-events-db',
    '.',
    dockerfile='infra/dbs/events-db/Dockerfile',
    only=[
        'src/schema/Schema/',
        'src/schema/MigrationRunner/',
        'libs/data_types/csharp/',
        'infra/dbs/events-db/',
    ],
    ignore=['**/bin', '**/obj'],
)

docker_build(
    'hellarch-files-db',
    '.',
    dockerfile='infra/dbs/files-db/Dockerfile',
    only=[
        'src/schema/Schema/',
        'src/schema/MigrationRunner/',
        'libs/data_types/csharp/',
        'infra/dbs/files-db/',
    ],
    ignore=['**/bin', '**/obj'],
)

docker_build(
    'hellarch-identity-db',
    '.',
    dockerfile='infra/dbs/identity-db/Dockerfile',
    only=[
        'src/schema/Schema/',
        'src/schema/MigrationRunner/',
        'libs/data_types/csharp/',
        'infra/dbs/identity-db/',
    ],
    ignore=['**/bin', '**/obj'],
)

# Label infrastructure resources
dc_resource('auth-db', labels=['infra'])
dc_resource('user-db', labels=['infra'])
dc_resource('events-db', labels=['infra'])
dc_resource('files-db', labels=['infra'])
dc_resource('identity-db', labels=['infra', 'identity'])
dc_resource('minio', labels=['infra'], links=['http://localhost:9001'])
dc_resource('redis', labels=['infra'])
dc_resource('rabbitmq', labels=['infra'])

# ============================================================================
# Services (rebuild on change)
# ============================================================================

docker_compose('./infra/docker/docker-compose.services.yml')

# Gateway (C#)
docker_build(
    'hellarch-gateway',
    'src/gateway',
    dockerfile='src/gateway/Dockerfile',
    ignore=['**/bin', '**/obj'],
)

# Auth service (Go)
docker_build(
    'hellarch-auth',
    '.',
    dockerfile='src/services/auth/Dockerfile',
    only=[
        'src/services/auth/',
        'libs/go/',
        'libs/data_types/go/',
        'libs/cloud_events/go/',
    ],
)

# User service (F#)
docker_build(
    'hellarch-user',
    '.',
    dockerfile='src/services/user/Dockerfile',
    only=[
        'src/services/user/',
        'libs/data_types/fsharp/',
        'libs/fsharp/httputil/',
        'libs/cloud_events/fsharp/',
        'libs/protos/',
    ],
    ignore=['**/bin', '**/obj'],
)

# Identity service (F#)
docker_build(
    'hellarch-identity',
    '.',
    dockerfile='src/services/identity/Dockerfile',
    only=[
        'src/services/identity/',
        'libs/data_types/fsharp/',
        'libs/fsharp/httputil/',
    ],
    ignore=['**/bin', '**/obj'],
)

# Admin client (C# Blazor)
docker_build(
    'hellarch-admin',
    '.',
    dockerfile='src/clients/Admin/Dockerfile',
    only=[
        'src/clients/Admin/',
        'libs/components/blazor/',
        'libs/cloud_events/csharp/',
        'libs/csharp/HellArch.Http/',
    ],
    ignore=['**/bin', '**/obj'],
)

# Health service (Gleam)
docker_build(
    'hellarch-health',
    'src/services/health',
    dockerfile='src/services/health/Dockerfile',
)

# Events service (Go)
docker_build(
    'hellarch-events',
    '.',
    dockerfile='src/services/events/Dockerfile',
    only=[
        'src/services/events/',
        'libs/go/',
        'libs/cloud_events/go/',
    ],
)

# Files service (Go)
docker_build(
    'hellarch-files',
    '.',
    dockerfile='src/services/files/Dockerfile',
    only=[
        'src/services/files/',
        'libs/go/',
        'libs/cloud_events/go/',
    ],
)

# Audit worker (Go)
docker_build(
    'hellarch-audit-worker',
    '.',
    dockerfile='src/workers/audit/Dockerfile',
    only=[
        'src/workers/audit/',
        'libs/go/',
        'libs/cloud_events/go/',
    ],
)

# Webhooks worker (Go)
docker_build(
    'hellarch-webhooks-worker',
    '.',
    dockerfile='src/workers/webhooks/Dockerfile',
    only=[
        'src/workers/webhooks/',
        'libs/go/',
        'libs/cloud_events/go/',
    ],
)

# Realtime worker (C#)
docker_build(
    'hellarch-realtime-worker',
    '.',
    dockerfile='src/workers/realtime/Dockerfile',
    only=[
        'src/workers/realtime/',
        'libs/csharp/HellArch.RabbitMQ/',
        'libs/cloud_events/csharp/',
    ],
    ignore=['**/bin', '**/obj'],
)

# Infra worker (Go)
docker_build(
    'hellarch-infra-worker',
    '.',
    dockerfile='src/workers/infra/Dockerfile',
    only=[
        'src/workers/infra/',
        'libs/go/',
        'libs/cloud_events/go/',
    ],
)

# DLQ worker (C#)
docker_build(
    'hellarch-dlq-worker',
    '.',
    dockerfile='src/workers/dlq/Dockerfile',
    only=[
        'src/workers/dlq/',
        'libs/csharp/HellArch.RabbitMQ/',
        'libs/cloud_events/csharp/',
    ],
    ignore=['**/bin', '**/obj'],
)

# Images worker (Go)
docker_build(
    'hellarch-images-worker',
    '.',
    dockerfile='src/workers/images/Dockerfile',
    only=[
        'src/workers/images/',
        'libs/go/',
        'libs/cloud_events/go/',
    ],
)

# Label service resources (DBs now run migrations on startup)
dc_resource('gateway', labels=['services'], resource_deps=['auth-db', 'redis', 'clients'], links=['http://localhost:5000', 'http://localhost:5000/app'])
dc_resource('auth', labels=['services'], resource_deps=['auth-db', 'redis', 'rabbitmq'], links=['http://localhost:5001'])
dc_resource('user', labels=['services'], resource_deps=['user-db', 'redis', 'rabbitmq'], links=['http://localhost:5002'])
dc_resource('admin', labels=['clients'], resource_deps=['auth', 'user', 'redis'], links=['http://localhost:5003'])
dc_resource('health', labels=['services'], resource_deps=['redis', 'rabbitmq', 'events'], links=['http://localhost:5004/health', 'http://localhost:5004/status'])
dc_resource('events', labels=['services'], resource_deps=['events-db', 'rabbitmq'], links=['http://localhost:5005'])

# Workers
dc_resource('audit-worker', labels=['workers'], resource_deps=['rabbitmq'])
dc_resource('webhooks-worker', labels=['workers'], resource_deps=['rabbitmq'])
dc_resource('realtime-worker', labels=['workers'], resource_deps=['rabbitmq', 'gateway'], links=['http://localhost:5020/health'])
dc_resource('infra-worker', labels=['workers'], resource_deps=['rabbitmq', 'health'])
dc_resource('dlq-worker', labels=['workers'], resource_deps=['rabbitmq', 'realtime-worker'], links=['http://localhost:5025/health'])
dc_resource('images-worker', labels=['workers'], resource_deps=['rabbitmq', 'minio', 'files'])

# Files service
dc_resource('files', labels=['services'], resource_deps=['files-db', 'minio', 'rabbitmq'], links=['http://localhost:5006'])

# Identity service (optional - for self-hosted auth)
dc_resource('identity', labels=['services', 'identity'], resource_deps=['identity-db'], links=['http://localhost:5010'])

# Frontend clients (Vue app with Vuetify)
# Built to src/gateway/Gateway/wwwroot/ - volume mount picks up changes immediately
local_resource(
    'clients',
    cmd='mise run clients:all',
    deps=[
        'src/clients/App/src',
        'src/clients/App/index.html',
        'src/clients/App/package.json',
        'src/clients/Identity/src',
        'src/clients/Identity/index.html',
        'src/clients/Identity/package.json',
    ],
    ignore=[
        '**/bin', '**/obj', '**/node_modules', '**/dist',
        '**/*.g.cs', '**/package-lock.json', '**/.build',
    ],
    labels=['clients'],
)

# ============================================================================
# Observability
# ============================================================================

docker_compose('./infra/docker/docker-compose.observability.yml')

dc_resource('jaeger', labels=['observability'], links=['http://localhost:16686'])

# ============================================================================
# Dev Tools
# ============================================================================

local_resource(
    'generate-types',
    cmd='cd libs/data_types && mise exec -- dotnet run --project Codegen/Codegen.csproj -- csharp .',
    deps=['libs/data_types/csharp'],
    labels=['tools'],
    auto_init=True
)

local_resource(
    'generate-protos',
    cmd='./scripts/generate-protos.sh',
    deps=['protos'],
    labels=['tools'],
    trigger_mode=TRIGGER_MODE_MANUAL
)

