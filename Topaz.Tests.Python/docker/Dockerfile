# Build context is the repository root so both sdk/python and test sources
# are accessible.  Called from PythonFixture.cs via ImageFromDockerfileBuilder.

FROM python:3.12-slim

# Install system CA store tooling for appending the Topaz self-signed cert
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*

# Install the topaz-sdk from the monorepo source
COPY sdk/python /sdk
RUN pip install --no-cache-dir /sdk

# Install test dependencies
RUN pip install --no-cache-dir \
    pytest==8.3.* \
    pytest-asyncio==0.24.* \
    azure-keyvault-secrets>=4.8.0 \
    azure-keyvault-keys>=4.9.0 \
    azure-keyvault-certificates>=4.8.0 \
    azure-mgmt-containerregistry>=10.3.0 \
    azure-storage-blob>=12.22.0 \
    azure-storage-queue>=12.11.0 \
    azure-data-tables>=12.5.0 \
    azure-servicebus>=7.12.0 \
    azure-mgmt-authorization>=4.0.0 \
    azure-mgmt-eventhub>=11.0.0 \
    azure-mgmt-msi>=7.0.0 \
    azure-mgmt-compute>=33.0.0 \
    azure-mgmt-network>=26.0.0 \
    azure-mgmt-web>=7.3.0 \
    azure-mgmt-resource>=23.1.0 \
    azure-mgmt-subscription>=3.1.0 \
    azure-eventhub>=5.11.0

# Copy Python test sources
COPY Topaz.Tests.Python/tests /tests

WORKDIR /tests

ENTRYPOINT ["tail", "-f", "/dev/null"]
