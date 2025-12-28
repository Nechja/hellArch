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
dc_resource('gateway', labels=['services'], resource_deps=['auth-db', 'redis'], links=['http://localhost:5000'])
dc_resource('auth', labels=['services'], resource_deps=['auth-db', 'redis', 'rabbitmq'], links=['http://localhost:5001'])
dc_resource('user', labels=['services'], resource_deps=['user-db', 'redis'], links=['http://localhost:5002'])
# Login client (Blazor WASM - runs locally with mise-managed dotnet)
local_resource(
    'login',
    serve_cmd='cd src/clients/Login && mise exec -- dotnet watch run --no-hot-reload',
    deps=['src/clients/Login'],
    ignore=['**/bin', '**/obj', '**/*.g.cs'],
    labels=['clients'],
    resource_deps=['gateway'],
    links=['http://localhost:5010']
)

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

