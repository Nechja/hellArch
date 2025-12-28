#!/bin/bash
# Reset dev databases for fresh testing

set -e

echo "Resetting auth database..."
docker exec hellarch-auth-db psql -U hellarch -d auth_db -c "
  TRUNCATE users, sessions, linked_accounts CASCADE;
"

echo "Resetting user database..."
docker exec hellarch-user-db psql -U hellarch -d user_db -c "
  TRUNCATE user_profiles, user_preferences CASCADE;
"

echo "Clearing Redis sessions..."
docker exec hellarch-redis redis-cli FLUSHDB

echo "Done! You can now log in fresh."
