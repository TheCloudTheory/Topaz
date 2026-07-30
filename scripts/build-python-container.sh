#!/bin/bash
# Builds the Python test container image used by Topaz.Tests.Python.
# The image is built for the current host architecture (no cross-compilation needed).
#
# Usage: ./scripts/build-python-container.sh
#
# Run this once before executing Topaz.Tests.Python tests, or whenever
# Topaz.Tests.Python/docker/Dockerfile or sdk/python/ changes.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Stage only the files the Dockerfile needs to avoid a multi-GB build context
CTX=$(mktemp -d)
trap 'rm -rf "$CTX"' EXIT
mkdir -p "$CTX/sdk" "$CTX/tests"
cp -r "$ROOT_DIR/sdk/python/." "$CTX/sdk/"
cp -r "$ROOT_DIR/Tests/Topaz.Tests.Python/tests/." "$CTX/tests/"
# Dockerfile must be inside the context so BuildKit resolves COPY paths correctly
cp "$ROOT_DIR/Tests/Topaz.Tests.Python/docker/Dockerfile" "$CTX/Dockerfile"

echo "Building topaz-python-test image from $ROOT_DIR/Tests/Topaz.Tests.Python/docker/Dockerfile..."
docker build \
    -t topaz-python-test \
    "$CTX"

echo "Build complete: topaz-python-test"
docker inspect topaz-python-test --format 'Architecture: {{.Architecture}}/{{.Os}}'
