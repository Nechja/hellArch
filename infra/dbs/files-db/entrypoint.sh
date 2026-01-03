#!/bin/bash
set -e

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

# Build connection string and run migrations
CONNECTION_STRING="Host=localhost;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"

if migrate files "$CONNECTION_STRING"; then
    echo "==> Migrations completed successfully"
else
    echo "==> Migration FAILED - stopping container"
    kill $POSTGRES_PID 2>/dev/null || true
    exit 1
fi

# Wait on postgres to keep container running
wait $POSTGRES_PID
