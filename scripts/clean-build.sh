#!/bin/bash
# Clean build artifacts (especially Docker-owned ones)

set -e

echo "Cleaning .NET build artifacts..."

# Find and remove all bin/obj directories
find . -type d \( -name "bin" -o -name "obj" \) -prune -exec rm -rf {} + 2>/dev/null || true

echo "Done! Build directories cleaned."
