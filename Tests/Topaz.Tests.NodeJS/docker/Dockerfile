# Build context is the repository root so test sources are accessible.
# Called from NodeJSFixture.cs via ImageFromDockerfileBuilder.

FROM node:22-slim

# Install system CA store tooling for appending the Topaz self-signed cert
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy package manifests and install dependencies
COPY tests/package.json tests/package-lock.json /tests/
RUN cd /tests && npm ci --omit=dev

# Copy test scripts
COPY tests/smoke-service-bus.mjs tests/smoke-event-hub.mjs /tests/

WORKDIR /tests

ENTRYPOINT ["tail", "-f", "/dev/null"]
