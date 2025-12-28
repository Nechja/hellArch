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
dc_resource('gateway', labels=['services'], resource_deps=['auth-db', 'redis', 'design-copy', 'login-copy', 'app-copy'], links=['http://localhost:5000', 'http://localhost:5000/login', 'http://localhost:5000/app'])
dc_resource('auth', labels=['services'], resource_deps=['auth-db', 'redis', 'rabbitmq'], links=['http://localhost:5001'])
dc_resource('user', labels=['services'], resource_deps=['user-db', 'redis', 'rabbitmq'], links=['http://localhost:5002'])
dc_resource('events-worker', labels=['workers'], resource_deps=['rabbitmq'])
dc_resource('admin', labels=['clients'], resource_deps=['auth', 'user', 'redis'], links=['http://localhost:5003'])
# Login client (Blazor WASM - builds and copies to Gateway wwwroot for serving)
local_resource(
    'login-build',
    cmd='mise exec -- dotnet publish src/clients/Login/Login.csproj -c Debug -o .tilt/login',
    deps=['src/clients/Login'],
    ignore=['**/bin', '**/obj', '**/*.g.cs'],
    labels=['clients'],
)

# Copy Login WASM output to Gateway wwwroot (runs after login-build)
local_resource(
    'login-copy',
    cmd='mkdir -p src/gateway/Gateway/wwwroot/login && cp -r .tilt/login/wwwroot/* src/gateway/Gateway/wwwroot/login/',
    resource_deps=['login-build'],
    labels=['clients'],
)

# Design system (shared between Vue and Blazor)
local_resource(
    'design-install',
    cmd='cd libs/design && npm install',
    deps=['libs/design/package.json'],
    labels=['design'],
)

local_resource(
    'design-build',
    cmd='mise run design:build',
    deps=['libs/design/uno.config.ts', 'libs/design/tokens.css'],
    resource_deps=['design-install'],
    labels=['design'],
)

local_resource(
    'design-copy',
    cmd='mise run design:copy',
    resource_deps=['design-build'],
    labels=['design'],
)

# Vue App client - builds and copies to Gateway wwwroot for serving
local_resource(
    'app-install',
    cmd='cd src/clients/App && npm install',
    deps=['src/clients/App/package.json'],
    resource_deps=['design-install'],
    labels=['clients'],
)

local_resource(
    'app-build',
    cmd='cd src/clients/App && npm run build',
    deps=['src/clients/App/src', 'src/clients/App/index.html', 'libs/design'],
    resource_deps=['app-install'],
    labels=['clients'],
)

local_resource(
    'app-copy',
    cmd='mkdir -p src/gateway/Gateway/wwwroot/app && cp -r src/clients/App/dist/* src/gateway/Gateway/wwwroot/app/',
    resource_deps=['app-build'],
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

