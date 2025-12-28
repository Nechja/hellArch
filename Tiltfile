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

# Label infrastructure resources
dc_resource('auth-db', labels=['infra'])
dc_resource('user-db', labels=['infra'])
dc_resource('redis', labels=['infra'])
dc_resource('rabbitmq', labels=['infra'])

# ============================================================================
# Services (containerized with hot reload via volume mounts)
# ============================================================================

docker_compose('./infra/docker/docker-compose.services.yml')

# Label service resources (DBs now run migrations on startup)
dc_resource('gateway', labels=['services'], resource_deps=['auth-db', 'redis', 'clients'], links=['http://localhost:5000', 'http://localhost:5000/login', 'http://localhost:5000/app'])
dc_resource('auth', labels=['services'], resource_deps=['auth-db', 'redis', 'rabbitmq'], links=['http://localhost:5001'])
dc_resource('user', labels=['services'], resource_deps=['user-db', 'redis', 'rabbitmq'], links=['http://localhost:5002'])
dc_resource('events-worker', labels=['workers'], resource_deps=['rabbitmq'])
dc_resource('admin', labels=['clients'], resource_deps=['auth', 'user', 'redis'], links=['http://localhost:5003'])
# All frontend clients (design system + login + app)
# Uses mise tasks for orchestration - see .mise.toml for details
local_resource(
    'clients',
    cmd='mise run clients:all',
    deps=[
        'libs/design/tokens.css',
        'libs/design/uno.config.ts',
        'src/clients/Login/Pages',
        'src/clients/Login/wwwroot',
        'src/clients/App/src',
        'src/clients/App/index.html',
    ],
    ignore=[
        '**/bin', '**/obj', '**/node_modules', '**/dist',
        '**/*.g.cs', '**/package-lock.json', '**/.build',
    ],
    labels=['clients'],
)

# Reload gateway after clients build to pick up new static files
local_resource(
    'clients-reload',
    cmd='docker compose -f infra/docker/docker-compose.services.yml restart gateway',
    resource_deps=['clients'],
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

