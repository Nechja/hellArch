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
# Services (containerized with hot reload via volume mounts)
# ============================================================================

docker_compose('./infra/docker/docker-compose.services.yml')

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

