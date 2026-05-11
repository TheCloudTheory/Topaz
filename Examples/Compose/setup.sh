#!/bin/sh
# Populates the topaz-certs Docker volume with the certificate files by using
# `docker cp` — the same mechanism Testcontainers' WithResourceMapping uses
# internally. This avoids any bind mount from the host filesystem.

set -e

VOLUME="topaz-certs"

echo "Creating volume $VOLUME..."
docker volume create "$VOLUME" > /dev/null

echo "Copying certificate files into the volume..."
CONTAINER=$(docker create -v "$VOLUME:/certs" alpine)
docker cp topaz.crt "$CONTAINER:/certs/topaz.crt"
docker cp topaz.key "$CONTAINER:/certs/topaz.key"
docker rm "$CONTAINER" > /dev/null

echo "Done. Run 'docker-compose up' to start the stack."
