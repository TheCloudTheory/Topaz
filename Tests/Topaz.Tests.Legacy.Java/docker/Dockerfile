# Build context: repository root
# Produces an image with Maven + the legacy azure-storage 7.x SDK tests baked in.

FROM maven:3.9-eclipse-temurin-11

# Install CA tooling
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*

COPY Topaz.Tests.Legacy.Java/tests /tests

RUN cd /tests && mvn -q dependency:resolve
