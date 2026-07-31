#!/bin/bash
# Builds the Java legacy test container image used by Topaz.Tests.Legacy.Java.
#
# Usage: ./scripts/build-java-container.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

CTX=$(mktemp -d)
trap 'rm -rf "$CTX"' EXIT
cp -r "$ROOT_DIR/Tests/Topaz.Tests.Legacy.Java/tests/." "$CTX/tests/"
# Dockerfile must be inside the context so BuildKit resolves COPY paths correctly
cp "$ROOT_DIR/Tests/Topaz.Tests.Legacy.Java/docker/Dockerfile" "$CTX/Dockerfile"

echo "Building topaz-java-legacy-test image from $ROOT_DIR/Tests/Topaz.Tests.Legacy.Java/docker/Dockerfile..."
docker build \
    -t topaz-java-legacy-test \
    "$CTX"

echo "Build complete: topaz-java-legacy-test"
docker inspect topaz-java-legacy-test --format 'Architecture: {{.Architecture}}/{{.Os}}'
