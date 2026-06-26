#!/bin/sh
# Populates the topaz-certs Docker volume with the certificate files.
# Run this once before `docker compose up`.

set -e

VOLUME="topaz-certs"
CERT_DIR="$(dirname "$0")/../"   # reuse the shared certs from Examples/Compose/

echo "Creating volume $VOLUME..."
docker volume create "$VOLUME" > /dev/null

echo "Copying certificate files into the volume..."
CONTAINER=$(docker create -v "$VOLUME:/certs" alpine)
docker cp "$CERT_DIR/topaz.crt" "$CONTAINER:/certs/topaz.crt"
docker cp "$CERT_DIR/topaz.key" "$CONTAINER:/certs/topaz.key"
docker rm "$CONTAINER" > /dev/null

echo "Done. Run 'docker compose up' to start the stack."
echo ""
echo "Once running, test the forward proxy from your host machine:"
echo "  curl -sk https://backend.azurewebsites.topaz.local.dev:8900/"
echo ""
echo "Or exec into any container on topaz-net:"
echo "  docker exec <container> curl -sk https://backend.azurewebsites.topaz.local.dev:8900/"
