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
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

CTX=$(mktemp -d)
trap 'rm -rf "$CTX"' EXIT
mkdir -p "$CTX/tests"
cp "$ROOT_DIR/Tests/Topaz.Tests.NodeJS/package.json" "$CTX/tests/"
cp "$ROOT_DIR/Tests/Topaz.Tests.NodeJS/package-lock.json" "$CTX/tests/"
cp "$ROOT_DIR/Tests/Topaz.Tests.NodeJS/smoke-service-bus.mjs" "$CTX/tests/"
cp "$ROOT_DIR/Tests/Topaz.Tests.NodeJS/smoke-event-hub.mjs" "$CTX/tests/"
# Dockerfile must be inside the context so BuildKit resolves COPY paths correctly
cp "$ROOT_DIR/Tests/Topaz.Tests.NodeJS/docker/Dockerfile" "$CTX/Dockerfile"

echo "Building topaz-nodejs-test image from $ROOT_DIR/Tests/Topaz.Tests.NodeJS/docker/Dockerfile..."
docker build \
    -t topaz-nodejs-test \
    "$CTX"

echo "Build complete: topaz-nodejs-test"
docker inspect topaz-nodejs-test --format 'Architecture: {{.Architecture}}/{{.Os}}'
