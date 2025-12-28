#!/bin/bash
# Decrypt SOPS secrets for local development
# Usage: eval $(./scripts/decrypt-secrets.sh)
#    or: source <(./scripts/decrypt-secrets.sh)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

SOPS_DIR="$ROOT_DIR/infra/sops"
SECRETS_FILE="$SOPS_DIR/secrets.enc.yaml"
KEY_FILE="$SOPS_DIR/age-key.txt"

if [ ! -f "$SECRETS_FILE" ]; then
    echo "# Error: Encrypted secrets file not found: $SECRETS_FILE" >&2
    exit 1
fi

if [ ! -f "$KEY_FILE" ]; then
    echo "# Error: Age key file not found: $KEY_FILE" >&2
    echo "# Run ./scripts/setup-dev.sh or get the key from a team member" >&2
    exit 1
fi

export SOPS_AGE_KEY_FILE="$KEY_FILE"

# Decrypt and output as shell exports
# Use mise exec if available, otherwise assume sops is in PATH
if command -v mise &> /dev/null; then
    mise exec -- sops decrypt --output-type dotenv "$SECRETS_FILE" | sed 's/^/export /'
else
    sops decrypt --output-type dotenv "$SECRETS_FILE" | sed 's/^/export /'
fi
