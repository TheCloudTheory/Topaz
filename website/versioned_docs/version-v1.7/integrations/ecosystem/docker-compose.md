---
sidebar_position: 4
slug: /ecosystem/docker-compose
description: Configure a docker-compose.yml to run Topaz alongside your application, wire up Azure SDK networking, and manage the full container lifecycle.
keywords: [topaz docker compose, azure emulator docker, local azure docker compose, topaz container networking]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# How to run Topaz with Docker Compose

This guide shows you how to configure a `docker-compose.yml` that runs Topaz alongside your application, with the networking required for Azure SDK clients to reach the emulator.

## Scenario

This guide walks through a realistic setup: an application that depends on two Azure services.

| Service | Topaz port | Topaz DNS suffix |
|---|---|---|
| Azure Key Vault | 8898 | `{vault}.vault.topaz.local.dev` |
| Azure Blob Storage | 8891 | `{account}.blob.storage.topaz.local.dev` |

The resource names used throughout this guide are:

| Resource | Name |
|---|---|
| Subscription | `00000000-0000-0000-0000-000000000001` |
| Resource group | `rg-my-app` |
| Key Vault | `kv-my-app` |
| Storage Account | `stmyapp001` |

Replace these with your own names — but keep them consistent between the Compose file, the provisioning code, and your application configuration.

A complete, runnable example is available in the Topaz repository under [`Examples/Compose/`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Compose).

## How Topaz DNS works in Docker Compose

Topaz data-plane clients (Key Vault SDK, Storage SDK, Docker daemon) resolve service hostnames like `kv-my-app.vault.topaz.local.dev` rather than talking to `localhost`. This is the same behaviour as real Azure.

Docker Compose has no automatic wildcard DNS resolution for `*.topaz.local.dev`. The solution is:

1. Assign Topaz a **fixed IP address** on a private bridge network.
2. Add `extra_hosts` entries to the app service that map each Topaz hostname to that fixed IP.

The Compose file below uses the subnet `172.28.0.0/16` and assigns Topaz the address `172.28.0.10`. You can use any private range that does not conflict with your existing networks.

:::warning[`extra_hosts` are per-service]

`extra_hosts` entries are only injected into the container they are declared on. They are **not** available inside the Topaz container itself. This matters for health checks — see [Startup ordering](#startup-ordering).

:::

## Prerequisites

### TLS certificate

Topaz exposes every endpoint over HTTPS using a self-signed certificate. You do **not** need to generate a certificate yourself — ready-to-use `topaz.crt` and `topaz.key` files are available from two places:

- **Repository**: [`certificate/`](https://github.com/TheCloudTheory/Topaz/tree/main/certificate) in the Topaz GitHub repo.
- **Release artifacts**: each [GitHub Release](https://github.com/TheCloudTheory/Topaz/releases) ships `topaz.crt` and `topaz.key` as downloadable assets.

Place `topaz.crt` and `topaz.key` next to your `docker-compose.yml`.

### Certificate injection with `setup.sh`

Topaz reads its certificate from a path **inside the container**. Bind-mounting individual certificate files from the host can be unreliable across operating systems and Docker configurations (on macOS with Docker Engine, bind-mounting a single file from an external drive creates a directory instead of a file). The recommended approach mirrors how Testcontainers handles file injection via `WithResourceMapping`: copy the files into a named Docker volume using `docker cp`.

Run this once before `docker compose up`:

```bash
#!/bin/sh
# setup.sh — run once before docker-compose up
set -e

VOLUME="topaz-certs"
docker volume create "$VOLUME" > /dev/null

CONTAINER=$(docker create -v "$VOLUME:/certs" alpine)
docker cp topaz.crt "$CONTAINER:/certs/topaz.crt"
docker cp topaz.key "$CONTAINER:/certs/topaz.key"
docker rm "$CONTAINER" > /dev/null

echo "Done. Run 'docker-compose up' to start the stack."
```

The `topaz-certs` volume is declared `external: true` in the Compose file — Compose will not try to create or delete it automatically, which means it survives `docker compose down`.

### Application TLS trust

Your application container also needs to trust the Topaz certificate. The approach differs by runtime:

- **.NET** — add the certificate to the OS store in your `Dockerfile` (Debian/Ubuntu base images):
  ```dockerfile
  COPY topaz.crt /usr/local/share/ca-certificates/topaz.crt
  RUN update-ca-certificates
  ```
  Azure SDK clients connect normally with no code-level TLS overrides.
- **Python** — set `SSL_CERT_FILE=/certs/topaz.crt` in the Compose environment.
- **Node.js** — set `NODE_EXTRA_CA_CERTS=/certs/topaz.crt` in the Compose environment.
- **Go / Terraform** — set `SSL_CERT_FILE=/certs/topaz.crt` in the Compose environment.

### Apple Silicon (linux/amd64 image)

The Topaz host image is built for `linux/amd64`. On Apple Silicon (arm64) machines, add `platform: linux/amd64` to the `topaz` service so Docker uses Rosetta to run it:

```yaml
topaz:
  image: thecloudtheory/topaz-host:latest
  platform: linux/amd64   # required on Apple Silicon
```

## docker-compose.yml

```yaml
networks:
  topaz-net:
    driver: bridge
    ipam:
      config:
        - subnet: "172.28.0.0/16"

volumes:
  topaz-data: {}      # persists Topaz resource state across restarts
  topaz-certs:        # populated by setup.sh (docker cp — no bind mounts)
    external: true

services:

  # --- Topaz emulator -----------------------------------------------------------
  topaz:
    image: thecloudtheory/topaz-host:latest
    platform: linux/amd64   # remove if your host is already amd64
    networks:
      topaz-net:
        ipv4_address: "172.28.0.10"
    ports:
      - "8899:8899"   # ARM / Resource Manager
      - "8898:8898"   # Key Vault
      - "8892:8892"   # Container Registry
      - "8891:8891"   # Blob Storage
    volumes:
      - topaz-certs:/certs:ro      # certificate files injected by setup.sh
      - topaz-data:/app/.topaz     # durable resource state
    command:
      - --certificate-file
      - /certs/topaz.crt
      - --certificate-key
      - /certs/topaz.key
      - --log-level
      - Information

  # --- Your application ---------------------------------------------------------
  app:
    build:
      context: .
      dockerfile: app/Dockerfile
    depends_on:
      - topaz
    ports:
      - "8080:8080"
    networks:
      - topaz-net
    environment:
      AZURE_TENANT_ID: "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
      # SSL_CERT_FILE: "/certs/topaz.crt"         # Python, Go, Terraform
      # NODE_EXTRA_CA_CERTS: "/certs/topaz.crt"   # Node.js

    extra_hosts:
      # Resource Manager
      - "topaz.local.dev:172.28.0.10"
      # Key Vault data-plane
      - "kv-my-app.vault.topaz.local.dev:172.28.0.10"
      # Blob Storage data-plane
      - "stmyapp001.blob.storage.topaz.local.dev:172.28.0.10"
      # Container Registry data-plane (add if needed)
      - "myregistry.cr.topaz.local.dev:172.28.0.10"
```

:::tip[Adding more resources]

Each new resource name needs a matching `extra_hosts` entry in the **app** service using the same IP:

```yaml
- "another-vault.vault.topaz.local.dev:172.28.0.10"
- "stanotheracct.blob.storage.topaz.local.dev:172.28.0.10"
```

:::

## Startup ordering

`depends_on: topaz` ensures the Topaz container is **started** before your app, but not that Topaz has finished initialising. Topaz prints `✓ Topaz is ready` to stdout when it is accepting connections, but Docker has no way to observe that automatically.

### Why a Compose `healthcheck` is not straightforward

The most common approach — adding a `healthcheck` to the `topaz` service and using `depends_on: condition: service_healthy` — requires a tool (`curl`, `bash`, `nc`) to be available inside the Topaz image. The Topaz image is a minimal .NET runtime image and does not include any of these tools, so any `CMD`- or `CMD-SHELL`-based healthcheck will always fail.

### Recommended pattern: retry loop in the application

The correct solution is to have your application retry the first request to Topaz until it succeeds. The `TheCloudTheory.Topaz.ResourceManager` package provides `TopazArmClient.CheckIfReadyAsync()` for exactly this purpose:

```csharp
using var topazClient = new TopazArmClient(credential);
for (var attempt = 1; ; attempt++)
{
    if (await topazClient.CheckIfReadyAsync()) break;
    if (attempt >= 20) throw new TimeoutException("Topaz did not become ready after 40 seconds.");
    Console.WriteLine($"[startup] Topaz not ready yet (attempt {attempt}/20), retrying in 2 s...");
    await Task.Delay(TimeSpan.FromSeconds(2));
}
// proceed with provisioning...
await topazClient.CreateSubscriptionAsync(subscriptionId, "dev-local");
```

`CheckIfReadyAsync()` calls the unauthenticated `GET /health` endpoint on the Resource Manager port (8899) and returns `true` when it receives a successful response. It catches `HttpRequestException` and returns `false` while Topaz is still starting.

## Provisioning resources at startup

The cleanest approach for a self-contained Compose stack is to provision resources programmatically inside your application at startup, using the Topaz ARM client and the Azure SDK. This means no separate provisioning step and the stack is fully reproducible with `docker compose up`.

```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

// Topaz superadmin object ID — grants unrestricted access during local development.
var credential = new AzureLocalCredential("00000000-0000-0000-0000-000000000000");
var subscriptionId = "00000000-0000-0000-0000-000000000001";

// 1. Wait for Topaz to be ready (see Startup ordering above).
using var topazClient = new TopazArmClient(credential);
// ... retry loop ...
await topazClient.CreateSubscriptionAsync(Guid.Parse(subscriptionId), "dev-local");

// 2. Create infrastructure via ARM SDK.
var armClient = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
var subscription = armClient.GetSubscriptionResource(
    SubscriptionResource.CreateResourceIdentifier(subscriptionId));

var rgResponse = await subscription.GetResourceGroups().CreateOrUpdateAsync(
    WaitUntil.Completed, "rg-my-app",
    new ResourceGroupData(AzureLocation.WestEurope));
var resourceGroup = rgResponse.Value;

await resourceGroup.GetKeyVaults().CreateOrUpdateAsync(
    WaitUntil.Completed, "kv-my-app",
    new KeyVaultCreateOrUpdateContent(
        AzureLocation.WestEurope,
        new KeyVaultProperties(Guid.Empty,
            new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(
    WaitUntil.Completed, "stmyapp001",
    new StorageAccountCreateOrUpdateContent(
        new StorageSku(StorageSkuName.StandardLrs),
        StorageKind.StorageV2,
        AzureLocation.WestEurope));
```

:::info[Surviving restarts]

The `topaz-data` volume persists `/app/.topaz` across `docker compose restart`. `CreateOrUpdateAsync` is idempotent — re-running provisioning against an already-populated volume is safe.

:::

## Connecting the Azure SDK

### Key Vault

```csharp
var kvEndpoint = TopazResourceHelpers.GetKeyVaultEndpoint("kv-my-app");
var secretClient = new SecretClient(kvEndpoint, credential);
```

### Blob Storage

You can connect to the Blob Storage data plane using either Shared Key (connection string) or a token credential. Token credential auth (`BlobServiceClient(Uri, TokenCredential)`) is fully supported — Topaz validates the Bearer token via its RBAC engine.

**Token credential (Entra ID-style auth):**

```csharp
var credential = new AzureLocalCredential(objectId);
var serviceUri = new Uri(TopazResourceHelpers.GetBlobServiceUri(storageAccountName));
var blobServiceClient = new BlobServiceClient(serviceUri, credential);
```

**Shared Key (connection string):**

```csharp
var storageAccount = await resourceGroup.GetStorageAccountAsync(storageAccountName);
var keys = storageAccount.Value.GetKeys().ToArray();
var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(
    storageAccountName, keys[0].Value);
var blobServiceClient = new BlobServiceClient(connectionString);
```

Use the token credential form when your real application uses managed identity or workload identity — it exercises the same RBAC path. Use the connection string form when you need Shared Key auth or are testing storage-level functionality independent of identity.

### Container Registry

To push or pull images from the emulated Container Registry, log in with the Docker CLI using the ACR admin credentials retrieved from Topaz:

```bash
# Retrieve admin credentials
az acr credential show --name myregistry

# Log in to the emulated registry
docker login myregistry.cr.topaz.local.dev:8892 \
  --username myregistry \
  --password <password-from-above>

# Tag and push a local image
docker tag my-image:latest myregistry.cr.topaz.local.dev:8892/my-image:latest
docker push myregistry.cr.topaz.local.dev:8892/my-image:latest
```

## Resetting state

The `topaz-data` volume stores all resource state. To wipe everything and start fresh:

```bash
docker compose down -v
./setup.sh
docker compose up
```

`-v` removes named volumes managed by Compose (`topaz-data`), but **not** the `topaz-certs` external volume. Re-run `setup.sh` only if you also removed `topaz-certs` manually.
