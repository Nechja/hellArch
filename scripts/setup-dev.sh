#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

echo "Setting up HellArch development environment..."

# ============================================================================
# Check prerequisites
# ============================================================================

echo ""
echo "Checking prerequisites..."

check_command() {
    if ! command -v "$1" &> /dev/null; then
        echo "  [MISSING] $1 - $2"
        return 1
    else
        echo "  [OK] $1"
        return 0
    fi
}

check_optional() {
    if ! command -v "$1" &> /dev/null; then
        echo "  [OPTIONAL] $1 not found - $2"
        return 1
    else
        echo "  [OK] $1"
        return 0
    fi
}

MISSING=0
check_command "docker" "https://docs.docker.com/get-docker/" || MISSING=1
check_command "tilt" "curl -fsSL https://raw.githubusercontent.com/tilt-dev/tilt/master/scripts/install.sh | bash" || MISSING=1

# Optional tools
HAS_MISE=0
HAS_DIRENV=0
HAS_SOPS=0
HAS_AGE=0
check_optional "mise" "curl https://mise.run | sh (manages tool versions)" && HAS_MISE=1
check_optional "direnv" "sudo apt install direnv (auto-loads environment)" && HAS_DIRENV=1
check_optional "sops" "mise install sops (encrypted secrets)" && HAS_SOPS=1
check_optional "age" "mise install age (encryption key management)" && HAS_AGE=1

if [ $MISSING -eq 1 ]; then
    echo ""
    echo "Required tools missing. Please install and run again."
    exit 1
fi

# ============================================================================
# Install tool versions (if mise available)
# ============================================================================

if [ $HAS_MISE -eq 1 ]; then
    echo ""
    echo "Installing tool versions via mise..."
    cd "$ROOT_DIR"
    mise install
else
    echo ""
    echo "Skipping mise (not installed). Ensure you have dotnet, go, and rust available."
fi

# ============================================================================
# Generate dev keys
# ============================================================================

echo ""
echo "Generating development RSA keys for JWT..."
KEYS_DIR="$ROOT_DIR/infra/keys"
mkdir -p "$KEYS_DIR"

if [ ! -f "$KEYS_DIR/dev-private.pem" ]; then
    openssl genrsa -out "$KEYS_DIR/dev-private.pem" 4096 2>/dev/null
    openssl rsa -in "$KEYS_DIR/dev-private.pem" -pubout -out "$KEYS_DIR/dev-public.pem" 2>/dev/null
    echo "  Generated dev-private.pem and dev-public.pem"
else
    echo "  Keys already exist"
fi

# ============================================================================
# Generate age key for SOPS (if age available)
# ============================================================================

if [ $HAS_AGE -eq 1 ]; then
    echo ""
    echo "Setting up age encryption key for SOPS..."
    SOPS_DIR="$ROOT_DIR/infra/sops"
    mkdir -p "$SOPS_DIR"

    if [ ! -f "$SOPS_DIR/age-key.txt" ]; then
        age-keygen -o "$SOPS_DIR/age-key.txt" 2>&1 | head -1
        echo "  Generated age-key.txt"
        echo "  IMPORTANT: Share this key securely with team members"
    else
        echo "  Age key already exists"
    fi
fi

# ============================================================================
# Setup .env
# ============================================================================

echo ""
if [ ! -f "$ROOT_DIR/.env" ]; then
    cp "$ROOT_DIR/.env.example" "$ROOT_DIR/.env"
    echo "Created .env from .env.example"
    echo "  NOTE: Edit .env and add your Discord OAuth credentials"
else
    echo ".env already exists"
fi

# ============================================================================
# Decrypt secrets (if SOPS available)
# ============================================================================

if [ $HAS_SOPS -eq 1 ] && [ -f "$SOPS_DIR/secrets.enc.yaml" ] && [ -f "$SOPS_DIR/age-key.txt" ]; then
    echo ""
    echo "Decrypting secrets..."
    export SOPS_AGE_KEY_FILE="$SOPS_DIR/age-key.txt"
    sops decrypt --output-type dotenv "$SOPS_DIR/secrets.enc.yaml" > "$ROOT_DIR/.env.secrets"
    echo "  Created .env.secrets from encrypted secrets"
else
    echo ""
    echo "Skipping secrets decryption (SOPS not available or no encrypted secrets)"
fi

# ============================================================================
# Allow direnv (if available)
# ============================================================================

if [ $HAS_DIRENV -eq 1 ]; then
    echo ""
    echo "Allowing direnv..."
    cd "$ROOT_DIR"
    direnv allow
fi

# ============================================================================
# Create Docker network
# ============================================================================

echo ""
echo "Creating Docker network 'hellarch'..."
docker network create hellarch 2>/dev/null || echo "  Network already exists"

# ============================================================================
# Start infrastructure
# ============================================================================

echo ""
echo "Starting infrastructure (Postgres, Redis, RabbitMQ)..."
cd "$ROOT_DIR"
docker compose -f infra/docker/docker-compose.infra.yml up -d

# ============================================================================
# Done
# ============================================================================

echo ""
echo "Setup complete!"
echo ""
echo "Next steps:"
echo "  1. Edit .env and add your Discord OAuth credentials"
echo "  2. Run 'tilt up' to start all services"
echo "  3. Open http://localhost:5000 for the Gateway"
echo "  4. Open http://localhost:10350 for Tilt dashboard"
echo ""
echo "All services run in Docker with hot reload via volume mounts."
echo "Databases are network-isolated (no host port exposure)."
echo ""
echo "To connect to a database for debugging:"
echo "  docker exec -it hellarch-auth-db psql -U hellarch -d auth_db"
echo "  docker exec -it hellarch-user-db psql -U hellarch -d user_db"
echo ""
