#!/bin/bash
# Builds the Node.js test container image used by Topaz.Tests.NodeJS.
# The image is built for the current host architecture (no cross-compilation needed).
#
# Usage: ./scripts/build-nodejs-container.sh
#
# Run this once before executing Topaz.Tests.NodeJS tests, or whenever
# Topaz.Tests.NodeJS/docker/Dockerfile or Topaz.Tests.NodeJS/package*.json changes.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$SCRIPT_DIR/.."

echo "Building topaz-nodejs-test image from $ROOT_DIR/Topaz.Tests.NodeJS/docker/Dockerfile..."
docker build \
    -f "$ROOT_DIR/Topaz.Tests.NodeJS/docker/Dockerfile" \
    -t topaz-nodejs-test \
    "$ROOT_DIR"

echo "Build complete: topaz-nodejs-test"
docker inspect topaz-nodejs-test --format 'Architecture: {{.Architecture}}/{{.Os}}'
