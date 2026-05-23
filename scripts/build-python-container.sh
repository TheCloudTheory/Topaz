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
ROOT_DIR="$SCRIPT_DIR/.."

echo "Building topaz-python-test image from $ROOT_DIR/Topaz.Tests.Python/docker/Dockerfile..."
docker build \
    -f "$ROOT_DIR/Topaz.Tests.Python/docker/Dockerfile" \
    -t topaz-python-test \
    "$ROOT_DIR"

echo "Build complete: topaz-python-test"
docker inspect topaz-python-test --format 'Architecture: {{.Architecture}}/{{.Os}}'
