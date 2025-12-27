#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PROTO_DIR="$ROOT_DIR/protos"

echo "Generating protos..."

# Go (Auth service)
echo "  -> Go (auth)"
protoc \
    --proto_path="$PROTO_DIR" \
    --go_out="$ROOT_DIR/src/services/auth/proto" \
    --go_opt=paths=source_relative \
    --go-grpc_out="$ROOT_DIR/src/services/auth/proto" \
    --go-grpc_opt=paths=source_relative \
    "$PROTO_DIR"/*.proto

# C# (Gateway, User service, Schema)
echo "  -> C# (gateway, user)"
# .NET uses Grpc.Tools via NuGet, so protos are compiled at build time
# This is just for reference - actual generation happens in .csproj

echo "Done!"
