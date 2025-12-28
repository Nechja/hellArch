#!/bin/bash
set -e

SCHEMA_DIR="/schema"
CONTEXT="UserDbContext"

# Start postgres in background using original entrypoint
docker-entrypoint.sh "$@" &
POSTGRES_PID=$!

echo "==> Waiting for PostgreSQL to be ready..."
until pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" -q 2>/dev/null; do
    # Check if postgres died
    if ! kill -0 $POSTGRES_PID 2>/dev/null; then
        echo "==> PostgreSQL process died"
        exit 1
    fi
    sleep 1
done
echo "==> PostgreSQL is ready"

# Run migrations
if [ -d "$SCHEMA_DIR" ]; then
    echo "==> Running EF Core migrations for $CONTEXT..."
    cd "$SCHEMA_DIR"

    if dotnet ef database update --context "$CONTEXT" --project Schema/Schema.csproj; then
        echo "==> Migrations completed successfully"
    else
        echo "==> Migration FAILED - stopping container"
        kill $POSTGRES_PID 2>/dev/null || true
        exit 1
    fi
else
    echo "==> Schema directory not mounted at $SCHEMA_DIR - skipping migrations"
fi

# Wait on postgres to keep container running
wait $POSTGRES_PID
