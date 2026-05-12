#!/usr/bin/env bash
# init-certs.sh — runs on the HOST (initializeCommand) before containers start.
# Populates a named Docker volume with the Topaz TLS certificate using docker cp,
# which works regardless of which paths are bind-mountable by the container runtime.

set -euo pipefail

VOLUME="topaz-devcontainer-certs"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

docker volume create "$VOLUME" > /dev/null

CONTAINER=$(docker create -v "$VOLUME:/certs" alpine)
docker cp "$SCRIPT_DIR/topaz.crt" "$CONTAINER:/certs/topaz.crt"
docker cp "$SCRIPT_DIR/topaz.key" "$CONTAINER:/certs/topaz.key"
docker rm "$CONTAINER" > /dev/null

echo "Topaz certs copied into volume: $VOLUME"
