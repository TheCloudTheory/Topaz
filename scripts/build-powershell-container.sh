#!/bin/bash
# Builds the PowerShell test container image used by Topaz.Tests.AzurePowerShell.
# The image is built for the current host architecture (no cross-compilation needed).
#
# Usage: ./scripts/build-powershell-container.sh
#
# Run this once before executing Topaz.Tests.AzurePowerShell tests, or whenever
# Dockerfile.powershell changes.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../Topaz.Tests.AzurePowerShell"

echo "Building topaz/powershell image from $PROJECT_DIR/Dockerfile.powershell..."
docker build \
    -f "$PROJECT_DIR/Dockerfile.powershell" \
    -t topaz/powershell \
    "$PROJECT_DIR"

echo "Build complete: topaz/powershell"
docker inspect topaz/powershell --format 'Architecture: {{.Architecture}}/{{.Os}}'
