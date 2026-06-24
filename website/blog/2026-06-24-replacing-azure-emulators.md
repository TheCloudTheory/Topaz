---
slug: replacing-azure-emulators-one-binary
title: "I replaced three Azure emulators with one binary, added Key Vault and ACR, and cut our CI setup to a single step"
description: How replacing Azurite, the Service Bus Emulator, and the Cosmos DB Emulator with Topaz eliminated three Docker images, three wait loops, and the Apple Silicon startup problem, while adding Key Vault, Container Registry, and Entra emulation.
keywords: [azure emulator replacement, azurite alternative, service bus emulator alternative, cosmos db emulator apple silicon, local azure development, topaz emulator, azure local ci]
authors: kamilmrzyglod
tags: [general, ci, devops]
---

For a while, local Azure development in my projects looked like this: Azurite for Blob and Queue Storage, the Microsoft Service Bus Emulator for messaging, the Cosmos DB Emulator for document storage, and nothing at all for Key Vault, Container Registry, or Entra. The last three were either skipped in local runs, mocked, or simply hit against a real Azure subscription when I needed them.

That worked, to a point. But the setup cost was real. Each emulator has its own Docker image, its own port range, its own quirks, and its own certificate story. Compose files grew. CI pipelines grew to match. And the services that had no emulator at all stayed untested locally, which is exactly the category where surprises tend to show up in production.

I wanted to see whether a single tool could cover the whole stack without making too many compromises.

{/* truncate */}

## What the old setup actually looked like

The previous CI compose file had three separate service containers. Azurite covered `BlobServiceClient`, `QueueServiceClient`, and `TableServiceClient`. The Service Bus Emulator added a second container with its own volume and a RabbitMQ dependency. The Cosmos DB Emulator added a third, and that one needed a minute and a half to initialise before tests could start on x64 machines. On Apple Silicon it did not start at all: the Linux container image is x64-only, and under Docker's Rosetta emulation layer the embedded database engine crashes during startup ([GitHub issue #54](https://github.com/Azure/azure-cosmos-db-emulator-docker/issues/54), open since July 2022 with 149 comments). There is a separate [vNext Linux emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux) that does run on ARM, but it is a different codebase still in preview with its own coverage gaps. In practice, anyone on an M-series Mac either skipped Cosmos DB local testing entirely, ran a Windows VM, or hit real Azure. Key Vault calls were either mocked or skipped regardless of platform.

The GitHub Actions workflow was about 120 lines and included three separate wait loops, one per emulator, each polling a different health endpoint on a different port. That is not the kind of complexity you notice until you have to debug it at 2am because a container did not start cleanly on a fresh runner image.

## What "one binary" means in practice

Topaz is a self-contained binary (or a Docker image if you prefer containers) that runs a local emulation layer covering:

- Blob Storage, Table Storage, Queue Storage (Azurite replacement)
- Service Bus with AMQP, AMQPS, and the `UseDevelopmentEmulator` SDK flag
- Cosmos DB (control plane + data plane, NoSQL API)
- Key Vault (secrets, keys, certificates, RBAC-controlled access)
- Container Registry (push, pull, login server, tasks)
- Azure Resource Manager (ARM, Bicep, Terraform deployments)
- Microsoft Entra ID tenant emulation (OIDC, ROPC, `az login`)
- RBAC (role assignments, permission checks, managed identity flows)

The install story on macOS is one command:

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host
```

On Linux there is a dedicated install script:

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash
topaz-host
```

Or pull the Docker image:

```bash
docker run -p 8899:8899 thecloudtheory/topaz-host
```

Once it is running, `GET /health` on port 8899 returns 200. That is the signal every CI script needs.

## The CI compose file now

Here is the compose file we use in GitHub Actions. The entire emulator layer is one service:

```yaml
services:
  topaz:
    image: thecloudtheory/topaz-host:latest
    platform: linux/amd64
    ports:
      - "443:443"     # HTTPS — required for MSAL pre-flight during az login
      - "8899:8899"   # ARM / Resource Manager  (health: GET /health)
      - "8898:8898"   # Key Vault data-plane
      - "8891:8891"   # Blob Storage data-plane
      - "8895:8895"   # Cosmos DB
      - "8889:8889"   # Service Bus AMQP plain
      - "5671:5671"   # Service Bus AMQPS / TLS
    volumes:
      - ../../certificate:/certs:ro
    command:
      - --certificate-file
      - /certs/topaz.crt
      - --certificate-key
      - /certs/topaz.key
      - --log-level
      - Warning
```

That is it. One service, one image, one wait loop on `/health`:

```bash
for i in $(seq 1 15); do
  if curl -sf https://topaz.local.dev:8899/health; then
    echo "Topaz is ready"
    break
  fi
  echo "Waiting... ($i/15)"
  sleep 2
done
```

## The workflow

The full GitHub Actions workflow is about 80 lines including comments. The relevant structure is:

1. Trust the Topaz self-signed certificate at the OS level and inside the Azure CLI Python bundle
2. Configure dnsmasq to resolve `*.topaz.local.dev` to `127.0.0.1` (one script, no per-resource `/etc/hosts` entries)
3. `docker compose up -d`
4. Poll `https://topaz.local.dev:8899/health` until it returns 200
5. Register Topaz as a custom Azure CLI cloud with `az cloud register`
6. `az login --username topazadmin@topaz.local.dev --password admin`
7. Run tests

Steps 1 through 3 replace the three container startup sequences plus the separate emulator configuration files we had before. Step 6 is new capability entirely. The old setup had no `az login` support at all because Azurite does not emulate Entra.

## SDK integration: no code changes

The pattern is the same regardless of language: swap the credential for `AzureLocalCredential` and point the client at the local endpoint. No mocks, no conditional branches.

**C# (.NET)**

```csharp
var credential = new AzureLocalCredential(Globals.GlobalAdminId);
var armClient  = new ArmClient(credential, subscriptionId);
```

The connection strings come back through `TopazResourceHelpers`, which resolves the correct local endpoint by resource name:

```csharp
var blobUri   = TopazResourceHelpers.GetBlobServiceUri(storageAccountName);
var kvUri     = TopazResourceHelpers.GetKeyVaultEndpoint(vaultName);
var cosmosUri = TopazResourceHelpers.GetCosmosDbAccountEndpoint(cosmosAccountName);
var sbConn    = TopazResourceHelpers.GetServiceBusConnectionStringWithTls(serviceBusNamespace);
```

**Python**

The `topaz_sdk` package provides the same primitives. The `azure-mgmt-*` clients accept a `base_url` and `credential_scopes` override so no monkey-patching is needed:

```python
from topaz_sdk import GLOBAL_ADMIN_ID, DEFAULT_RESOURCE_MANAGER_PORT, AzureLocalCredential

BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

def mgmt_kwargs(subscription_id):
    return {
        "credential": AzureLocalCredential(GLOBAL_ADMIN_ID),
        "subscription_id": subscription_id,
        "base_url": BASE_URL,
        "credential_scopes": [f"{BASE_URL}/.default"],
    }

# Key Vault data plane — point at the per-vault subdomain
secret_client = SecretClient(
    vault_url=f"https://{vault_name}.vault.topaz.local.dev:8898",
    credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
    verify_challenge_resource=False,
)
secret_client.set_secret("db-password", "s3cr3t-password")
```

No environment variable juggling. No conditional `if isDevelopment` branches scattered across startup.

## RBAC works too

This is the part I did not expect to be useful until I actually needed it. The SecretsRbac example in the repo shows the pattern: create a Key Vault, assign the `Key Vault Secrets User` built-in role to a managed identity principal, and then verify that the identity can read the secret while an unassigned identity gets a 403.

```csharp
await armClient
    .GetRoleAssignments(kvScope)
    .CreateOrUpdateAsync(
        WaitUntil.Completed,
        Guid.NewGuid().ToString(),
        new RoleAssignmentCreateOrUpdateContent(
            new ResourceIdentifier($"/providers/Microsoft.Authorization/roleDefinitions/{kvSecretsUserRoleId}"),
            assignedPrincipalId)
        {
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal
        });
```

Azure RBAC is one of those areas where the gap between a mock and real behavior shows up fastest. The mock returns 200. The real service returns 403 unless the exact scope hierarchy, role definition ID, and principal type are right. Topaz enforces the same checks locally, which means tests that pass locally actually mean something.

## Coverage gaps worth knowing about

Cosmos DB data plane covers the NoSQL API. Mongo API and Table API users will hit gaps. SQL and App Service are control-plane only for now. Everything else in the list above, including Event Hubs messaging, works end-to-end. The [API coverage docs](https://topaz.thecloudtheory.com/docs/category/api-coverage/) have the full operation-level breakdown if you want to check a specific call before migrating.

## The before and after

Before:

- 3 Docker images in CI compose (Azurite, Service Bus Emulator, Cosmos DB Emulator)
- 3 wait loops on 3 different ports
- No `az login`, no Key Vault, no ACR in integration tests
- Cosmos DB emulator does not start on Apple Silicon at all
- Service Bus namespace provisioning on real Azure: 60-120 seconds, occasionally longer

After (measured on a GitHub-hosted `ubuntu-latest` runner):

- 1 Docker image, 1 wait loop
- Full Entra login, Key Vault reads with RBAC enforcement, Container Registry push/pull
- Topaz ready in 7 seconds (5s start + 2s health poll)
- Full job including DNS setup, provisioning, and tests: **38 seconds**
- Works identically on Apple Silicon, x64, and CI runners

## Where to go from here

The compose file, the workflow YAML, and the `AllInOne` example are all in the [Topaz GitHub repo](https://github.com/TheCloudTheory/Topaz) under `Examples/`. The `brew install topaz` path works for local development; the Docker path works for CI.

Azurite is one starting point, but the more interesting cases are the ones that had no local option at all. If your application touches Key Vault, Container Registry, Entra, RBAC, or ARM deployments, you are probably either skipping those paths in tests, mocking them, or incurring real Azure costs in CI. Topaz is designed for the full stack, not as a drop-in for a single service.
