---
sidebar_position: 4
slug: /ecosystem/docker-compose
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Docker Compose

Docker Compose is a natural fit for local development with Topaz. A single `docker-compose.yml` file describes both Topaz and your application as services, wires up the networking that Azure SDK clients expect, and manages the full container lifecycle.

## Scenario

This guide walks through a realistic setup: an application that depends on three Azure services.

| Service | Topaz port | Topaz DNS suffix |
|---|---|---|
| Azure Key Vault | 8898 | `{vault}.vault.topaz.local.dev` |
| Azure Blob Storage | 8891 | `{account}.blob.storage.topaz.local.dev` |
| Azure Container Registry | 8892 | `{registry}.cr.topaz.local.dev` |

The resource names used throughout this guide are:

| Resource | Name |
|---|---|
| Subscription | `00000000-0000-0000-0000-000000000001` |
| Resource group | `rg-my-app` |
| Key Vault | `kv-my-app` |
| Storage Account | `stmyapp001` |
| Container Registry | `myregistry` |

Replace these with your own names — but keep them consistent between the Compose file, the provisioning commands, and your application configuration.

## How Topaz DNS works in Docker Compose

Topaz data-plane clients (Key Vault SDK, Storage SDK, Docker daemon) resolve service hostnames like `kv-my-app.vault.topaz.local.dev` rather than talking to `localhost`. This is the same behaviour as real Azure.

In Docker Compose there is no automatic wildcard DNS resolution for `*.topaz.local.dev`. The solution is:

1. Assign Topaz a **fixed IP address** on a private bridge network.
2. Add `extra_hosts` entries to the app service that map each Topaz hostname to that fixed IP.

The Compose file below uses the subnet `172.28.0.0/16` and assigns Topaz the address `172.28.0.10`. You can use any private range that does not conflict with your existing networks.

## Prerequisites

### TLS certificate

Topaz exposes every endpoint over HTTPS using a self-signed certificate. You do **not** need to generate a certificate yourself — ready-to-use `topaz.crt` and `topaz.key` files are available from two places:

- **Repository**: [`certificate/`](https://github.com/TheCloudTheory/Topaz/tree/main/certificate) in the Topaz GitHub repo — download and place them next to your `docker-compose.yml`.
- **Release artifacts**: each [GitHub Release](https://github.com/TheCloudTheory/Topaz/releases) ships `topaz.crt` and `topaz.key` as downloadable assets.

If you prefer to generate your own (for example to use a different Common Name), run:

```bash
bash certificate/generate.sh
```

However you obtain them, place `topaz.crt` and `topaz.key` next to your `docker-compose.yml`. The Compose file mounts both files into the Topaz container.

Your application also needs to trust this certificate. The approach differs by runtime:

- **.NET** — add the certificate to the system store inside your container image (e.g. `update-ca-certificates` on Debian/Ubuntu). The Azure SDK clients will then connect normally without any code-level workarounds.
- **Python** — set `SSL_CERT_FILE=/certs/topaz.crt` (shown in the Compose file).
- **Node.js** — set `NODE_EXTRA_CA_CERTS=/certs/topaz.crt`.
- **Go / Terraform** — set `SSL_CERT_FILE=/certs/topaz.crt`.

## docker-compose.yml

```yaml
networks:
  topaz-net:
    driver: bridge
    ipam:
      config:
        - subnet: "172.28.0.0/16"

volumes:
  topaz-data: {}   # persists Topaz resource state across restarts

services:

  # --- Topaz emulator -----------------------------------------------------------
  topaz:
    image: thecloudtheory/topaz-host:latest   # pin to a specific tag for reproducibility
    networks:
      topaz-net:
        ipv4_address: "172.28.0.10"
    ports:
      - "8899:8899"   # ARM / Resource Manager
      - "8898:8898"   # Key Vault
      - "8892:8892"   # Container Registry
      - "8891:8891"   # Blob Storage
    volumes:
      - ./topaz.crt:/app/topaz.crt:ro   # from certificate/generate.sh
      - ./topaz.key:/app/topaz.key:ro
      - topaz-data:/app/.topaz           # durable resource state
    command:
      - --certificate-file
      - topaz.crt
      - --certificate-key
      - topaz.key
      - --log-level
      - Information

  # --- Your application ---------------------------------------------------------
  app:
    image: my-app:latest   # replace with your application image, or add a `build:` section
    depends_on:
      - topaz             # Topaz starts in ~1 s; add retry logic in your app for robustness
    networks:
      - topaz-net
    volumes:
      - ./topaz.crt:/certs/topaz.crt:ro   # mount cert so the app runtime can trust it
    environment:
      # -- Azure identity -------------------------------------------------------
      AZURE_SUBSCRIPTION_ID: "00000000-0000-0000-0000-000000000001"
      AZURE_TENANT_ID: "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"   # Topaz default tenant

      # -- Service endpoints (read by your app to construct SDK clients) --------
      KEY_VAULT_ENDPOINT: "https://kv-my-app.vault.topaz.local.dev:8898"
      STORAGE_ACCOUNT_NAME: "stmyapp001"
      STORAGE_BLOB_ENDPOINT: "https://stmyapp001.blob.storage.topaz.local.dev:8891/"
      ACR_LOGIN_SERVER: "myregistry.cr.topaz.local.dev:8892"

      # -- TLS trust (adjust the env-var name for your runtime) ----------------
      SSL_CERT_FILE: "/certs/topaz.crt"         # Go, Python, Terraform
      # NODE_EXTRA_CA_CERTS: "/certs/topaz.crt" # Node.js (uncomment if needed)

    extra_hosts:
      # Resource Manager
      - "topaz.local.dev:172.28.0.10"
      # Key Vault data-plane
      - "kv-my-app.vault.topaz.local.dev:172.28.0.10"
      # Blob Storage data-plane
      - "stmyapp001.blob.storage.topaz.local.dev:172.28.0.10"
      # Container Registry data-plane
      - "myregistry.cr.topaz.local.dev:172.28.0.10"
```

:::tip[Adding more resources]

Each new resource name needs a matching `extra_hosts` entry using the same IP:

```yaml
- "another-vault.vault.topaz.local.dev:172.28.0.10"
- "stanotheracct.blob.storage.topaz.local.dev:172.28.0.10"
- "anotherregistry.cr.topaz.local.dev:172.28.0.10"
```

:::

## Initial provisioning

Resources in Topaz are not created automatically when the container starts. Run the provisioning commands once after the first `docker compose up`. You can use the Azure CLI (configured to target Topaz) or the Topaz CLI.

<Tabs>
<TabItem value="az" label="Azure CLI" default>

```bash
# Register and select the Topaz cloud
az cloud register -n Topaz --cloud-config cloud.json
az cloud set -n Topaz

# Authenticate against Topaz
az login --username topazadmin@topaz.local.dev --password admin

SUBSCRIPTION_ID="00000000-0000-0000-0000-000000000001"
RESOURCE_GROUP="rg-my-app"

az account set --subscription $SUBSCRIPTION_ID

# Resource group
az group create \
  --name $RESOURCE_GROUP \
  --location westeurope

# Key Vault
az keyvault create \
  --name kv-my-app \
  --resource-group $RESOURCE_GROUP \
  --location westeurope

# Storage Account
az storage account create \
  --name stmyapp001 \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_LRS \
  --location westeurope

# Container Registry
az acr create \
  --name myregistry \
  --resource-group $RESOURCE_GROUP \
  --sku Basic \
  --admin-enabled true
```

</TabItem>
<TabItem value="topaz" label="Topaz CLI">

```bash
SUBSCRIPTION_ID="00000000-0000-0000-0000-000000000001"
RESOURCE_GROUP="rg-my-app"

# Create a subscription (once per environment)
topaz subscription create \
  --subscription-id $SUBSCRIPTION_ID \
  --display-name dev-local

# Resource group
topaz resource-group create \
  --subscription-id $SUBSCRIPTION_ID \
  --name $RESOURCE_GROUP \
  --location westeurope

# Key Vault
topaz keyvault create \
  --subscription-id $SUBSCRIPTION_ID \
  --resource-group $RESOURCE_GROUP \
  --name kv-my-app \
  --location westeurope

# Storage Account
topaz storage account create \
  --subscription-id $SUBSCRIPTION_ID \
  --resource-group $RESOURCE_GROUP \
  --name stmyapp001 \
  --sku Standard_LRS \
  --location westeurope

# Container Registry
topaz acr create \
  --subscription-id $SUBSCRIPTION_ID \
  --resource-group $RESOURCE_GROUP \
  --name myregistry \
  --sku Basic \
  --admin-enabled true
```

</TabItem>
</Tabs>

:::info[Surviving restarts]

Because the `topaz-data` volume persists `/app/.topaz`, resources you create survive `docker compose restart`. You only need to re-run the provisioning commands when the volume is removed (see [Resetting state](#resetting-state)).

:::

## Connecting the Azure SDK

Configure your application's SDK clients to point at the Topaz endpoints injected via environment variables.

<Tabs>
<TabItem value="dotnet" label=".NET" default>

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;

// The Topaz certificate is trusted at the OS level inside the container
// (see the Dockerfile snippet below), so no custom transport is needed.

// Key Vault
var kvEndpoint = new Uri(Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT")!);
var secretClient = new SecretClient(kvEndpoint, new DefaultAzureCredential());

// Blob Storage
var blobEndpoint = new Uri(Environment.GetEnvironmentVariable("STORAGE_BLOB_ENDPOINT")!);
var blobServiceClient = new BlobServiceClient(blobEndpoint, new DefaultAzureCredential());
```

To trust the certificate at the OS level, add these lines to your app's `Dockerfile`:

```dockerfile
COPY topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates
```

For Alpine-based images use `update-ca-trust` after placing the cert in `/etc/pki/ca-trust/source/anchors/`.

</TabItem>
<TabItem value="python" label="Python">

```python
import os
from azure.keyvault.secrets import SecretClient
from azure.storage.blob import BlobServiceClient
from azure.identity import DefaultAzureCredential

# SSL_CERT_FILE is set in the Compose environment, so requests/urllib3 will
# pick up the Topaz certificate automatically — no extra configuration needed.

credential = DefaultAzureCredential()

# Key Vault
kv_endpoint = os.environ["KEY_VAULT_ENDPOINT"]
secret_client = SecretClient(vault_url=kv_endpoint, credential=credential)

# Blob Storage
blob_endpoint = os.environ["STORAGE_BLOB_ENDPOINT"]
blob_service_client = BlobServiceClient(account_url=blob_endpoint, credential=credential)
```

</TabItem>
<TabItem value="node" label="Node.js">

```typescript
import { SecretClient } from "@azure/keyvault-secrets";
import { BlobServiceClient } from "@azure/storage-blob";
import { DefaultAzureCredential } from "@azure/identity";

// NODE_EXTRA_CA_CERTS is set in the Compose environment so the Node TLS
// stack trusts the Topaz certificate automatically.

const credential = new DefaultAzureCredential();

// Key Vault
const kvEndpoint = process.env.KEY_VAULT_ENDPOINT!;
const secretClient = new SecretClient(kvEndpoint, credential);

// Blob Storage
const blobEndpoint = process.env.STORAGE_BLOB_ENDPOINT!;
const blobServiceClient = new BlobServiceClient(blobEndpoint, credential);
```

</TabItem>
</Tabs>

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

Your app container can pull from the same registry by configuring its image reference to use the Topaz login server, or by calling the ACR data-plane SDK:

```bash
docker pull myregistry.cr.topaz.local.dev:8892/my-image:latest
```

## Resetting state

The `topaz-data` volume stores all resource state. To wipe everything and start fresh:

```bash
docker compose down -v
docker compose up
```

Then re-run the [provisioning commands](#initial-provisioning).
