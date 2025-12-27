# -*- mode: Python -*-

# Load extensions
load('ext://dotenv', 'dotenv')

# Load environment from .env
dotenv()

# ============================================================================
# Infrastructure (network-only, no exposed ports)
# ============================================================================

docker_compose('./infra/docker/docker-compose.infra.yml')

# Label infrastructure resources
dc_resource('auth-db', labels=['infra'])
dc_resource('user-db', labels=['infra'])
dc_resource('redis', labels=['infra'])
dc_resource('rabbitmq', labels=['infra'])

# ============================================================================
# Services (containerized with hot reload via volume mounts)
# ============================================================================

docker_compose('./infra/docker/docker-compose.services.yml')

# Label service resources
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
    'generate-protos',
    cmd='./scripts/generate-protos.sh',
    deps=['protos'],
    labels=['tools'],
    trigger_mode=TRIGGER_MODE_MANUAL
)

# ============================================================================
# TODO: Uncomment when Schema project exists
# ============================================================================

# local_resource(
#     'migrate-auth',
#     cmd='cd src/schema && dotnet ef database update --context AuthDbContext',
#     deps=['src/schema/Migrations/Auth'],
#     labels=['migrations'],
#     resource_deps=['auth-db'],
#     auto_init=True,
#     trigger_mode=TRIGGER_MODE_MANUAL
# )
#
# local_resource(
#     'migrate-user',
#     cmd='cd src/schema && dotnet ef database update --context UserDbContext',
#     deps=['src/schema/Migrations/User'],
#     labels=['migrations'],
#     resource_deps=['user-db'],
#     auto_init=True,
#     trigger_mode=TRIGGER_MODE_MANUAL
# )
