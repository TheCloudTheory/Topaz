---
sidebar_position: 3
description: Looking for a Cosmos DB Emulator alternative? Topaz emulates Azure Cosmos DB NoSQL API alongside Storage, Key Vault, Service Bus, and more — in a single binary that runs on Apple Silicon without Rosetta workarounds.
keywords: [cosmos db emulator alternative, cosmos db emulator replacement, cosmos db emulator apple silicon, azure cosmos db local development, topaz vs cosmos db emulator, cosmos db linux emulator, local cosmos db]
---

# Azure Cosmos DB Emulator alternative

If you are using the Azure Cosmos DB Emulator today and running into platform or setup limitations, Topaz is an alternative that covers the Cosmos DB NoSQL API and adds the ARM control plane — alongside Storage, Key Vault, Service Bus, and the rest of the Azure stack — in a single process.

## What is the Azure Cosmos DB Emulator?

The [Azure Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator) is Microsoft's local development environment for Cosmos DB. The original Windows version has been available for years. The Linux containerised version is the variant used in CI pipelines and macOS environments.

The Linux emulator image is x64-only. On Apple Silicon Macs, the embedded database engine crashes during startup under Docker's Rosetta emulation layer ([GitHub issue #54](https://github.com/Azure/azure-cosmos-db-emulator-docker/issues/54), open since July 2022). Microsoft has acknowledged this and published a separate [vNext Linux emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux) that does run on ARM64, but it is a different codebase still in preview with its own coverage gaps. In practice, anyone on an M-series Mac either skipped Cosmos DB local testing entirely, ran a Windows VM, or hit real Azure.

On x64 Linux, the emulator starts but requires approximately 90 seconds to initialise before it can accept connections. In a CI pipeline, this is a mandatory sleep or a polling loop that adds latency to every run.

Topaz is written in .NET 10 and ships as a single binary or Docker image. It runs on both x64 and ARM64 without any platform-specific workarounds, starts in under 10 seconds, and emulates Cosmos DB alongside the broader Azure platform.

## Architecture and setup

| | Topaz | Cosmos DB Emulator (Linux) |
|---|---|---|
| **Platform support** | x64, ARM64 (native) | x64 only; ARM64 preview via separate image |
| **Container dependencies** | None | Self-contained Docker image |
| **Startup time** | ~7 seconds | ~90 seconds on x64 |
| **Runtime** | .NET 10 | Linux container (x64) |
| **License** | Apache 2.0 | Proprietary (Microsoft) |
| **Data persistence across restarts** | ✅ | ✅ |

## Feature comparison

| Feature | Topaz | Cosmos DB Emulator |
|---|---|---|
| **NoSQL API (SQL API)** | ✅ | ✅ |
| **MongoDB API** | ❌ | ✅ |
| **Table API** | ❌ | ✅ |
| **Gremlin (Graph) API** | ❌ | ✅ |
| **Cassandra API** | ❌ | ✅ |
| **Database and container CRUD** | ✅ | ✅ |
| **Documents** (create, read, replace, delete, query) | ✅ | ✅ |
| **SQL query support** | ✅ | ✅ |
| **Partition key support** | ✅ | ✅ |
| **Multiple accounts** | ✅ (unlimited, via ARM) | ❌ (one account per container) |
| **ARM control plane** | ✅ | ❌ |
| **Azure CLI (`az cosmosdb`)** | ✅ | ❌ |
| **Terraform (`azurerm` provider)** | ✅ | ❌ |
| **Entra ID / managed identity** | ✅ | ❌ |
| **RBAC (role-based access control)** | ✅ | ❌ |
| **Data Explorer UI** | ❌ | ✅ |
| **TLS / HTTPS** | ✅ | ✅ |
| **Co-located with other Azure services** | ✅ | ❌ |

## Apple Silicon and ARM64

This is the most immediate blocker for teams using M-series Macs. The Azure Cosmos DB Emulator Linux image requires x64 and does not run under Docker Desktop's Rosetta emulation layer on Apple Silicon — the embedded database engine crashes at startup. This has been an open issue since mid-2022.

Microsoft provides a vNext Linux emulator as a workaround, but it is a different codebase in preview with its own coverage gaps, and it requires a separate image tag and configuration.

Topaz runs natively on ARM64. There are no separate image tags, no Rosetta configuration, and no startup crash. The same Docker image and the same compose file work on developer laptops and CI runners regardless of architecture.

## Startup time

On a GitHub-hosted `ubuntu-latest` runner, the Cosmos DB Emulator takes approximately 90 seconds to reach a state where it can accept connections. The standard pattern is a polling loop that checks the emulator's health endpoint with `--insecure` on a self-signed TLS connection. This is a fixed cost on every CI run, and any flakiness in the emulator's internal initialization adds more.

Topaz is ready in approximately 7 seconds. The same health-check loop that waits for other Azure services (`GET /health` on port 8899) applies without modification.

## ARM control plane

The Azure Cosmos DB Emulator does not implement the Azure Resource Manager API. It exposes a proprietary data-plane endpoint that the Cosmos DB SDK connects to directly. Consequently:

- `az cosmosdb create` cannot target the emulator — the command calls `management.azure.com`
- Terraform's `azurerm_cosmosdb_account` and related resources target the ARM API and cannot be applied against the emulator

Topaz implements the ARM control plane for Cosmos DB. The same `az cosmosdb` commands and the same Terraform resource blocks that work against real Azure work locally against Topaz without modification:

```bash
az cosmosdb create \
  --name my-cosmos-account \
  --resource-group my-rg \
  --locations regionName=westeurope \
  --endpoint https://topaz.local.dev:8899

az cosmosdb sql database create \
  --account-name my-cosmos-account \
  --resource-group my-rg \
  --name my-database \
  --endpoint https://topaz.local.dev:8899
```

```hcl
resource "azurerm_cosmosdb_account" "example" {
  name                = "my-cosmos-account"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.example.location
    failover_priority = 0
  }
}
```

## SDK integration

The Cosmos DB SDK connects to Topaz the same way it connects to any non-default endpoint — override the account endpoint and provide a credential. No mocks, no conditional branches.

**C# (.NET)**

```csharp
var credential  = new AzureLocalCredential(Globals.GlobalAdminId);
var cosmosClient = new CosmosClient(
    TopazResourceHelpers.GetCosmosDbAccountEndpoint(accountName),
    credential);
```

**Python**

```python
from topaz_sdk import GLOBAL_ADMIN_ID, AzureLocalCredential
from azure.cosmos import CosmosClient

client = CosmosClient(
    url=f"https://{account_name}.documents.topaz.local.dev:8895",
    credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
)
```

## Multiple accounts

The Cosmos DB Emulator runs one account per container instance with a fixed endpoint (`https://localhost:8081`). Creating a second account requires a second container on a different port.

Topaz creates as many named accounts as needed through the ARM API:

```bash
az cosmosdb create --name cosmos-orders --resource-group my-rg --locations regionName=westeurope
az cosmosdb create --name cosmos-events --resource-group my-rg --locations regionName=westeurope
```

Each account gets its own subdomain (`cosmos-orders.documents.topaz.local.dev`) with DNS registered automatically — no hosts file edits.

## Coverage gaps worth knowing about

Topaz's Cosmos DB implementation covers the NoSQL (SQL) API for document CRUD and query operations. Applications that use the MongoDB API, Table API, Gremlin API, or Cassandra API will hit gaps. The [API coverage docs](https://topaz.thecloudtheory.com/docs/category/api-coverage/) list the supported operations at the call level.

## Beyond Cosmos DB

The Azure Cosmos DB Emulator is scoped entirely to Cosmos DB. Topaz emulates the broader Azure platform in a single process:

| Service | Topaz | Cosmos DB Emulator |
|---|---|---|
| Cosmos DB (NoSQL API) | ✅ | ✅ |
| Azure Storage (Blob, Table, Queue) | ✅ | ❌ |
| Key Vault (secrets, keys, certificates) | ✅ | ❌ |
| Service Bus (AMQP + HTTPS) | ✅ | ❌ |
| Event Hubs (AMQP + HTTPS) | ✅ | ❌ |
| Container Registry (push, pull, tags) | ✅ | ❌ |
| Managed Identity | ✅ | ❌ |
| Entra ID (local token issuance) | ✅ | ❌ |
| RBAC (role assignments) | ✅ | ❌ |
| ARM control plane (resource groups, subscriptions) | ✅ | ❌ |
| ARM template / Bicep deployments | ✅ | ❌ |
| Terraform `azurerm` provider target | ✅ | ❌ |
| Azure CLI (`az cosmosdb`, `az keyvault`, …) | ✅ | ❌ |
| MCP server for AI tooling | ✅ | ❌ |

## When to keep the emulator

The Azure Cosmos DB Emulator is the right choice if:

- Your application uses the MongoDB, Table, Gremlin, or Cassandra API
- You need the built-in Data Explorer UI for manual data browsing
- You are on x64 Linux or Windows and the 90-second startup cost is acceptable
- You have existing pipelines built around the emulator and migration is not worth the effort

## When to switch to Topaz

Topaz is the right choice if:

- You are on Apple Silicon and cannot get the Cosmos DB Emulator to start
- Your application uses the NoSQL (SQL) API and you want CI startup under 10 seconds
- You need `az cosmosdb` commands or Terraform `azurerm_cosmosdb_*` resources to work locally
- Your application uses Cosmos DB alongside other Azure services and you want a single process instead of multiple containers
- You want Entra ID and RBAC-enforced access to Cosmos DB in local and CI environments
- You want Apache 2.0 open-source software with no proprietary license restrictions

## Migrating from the emulator

Topaz implements the same Cosmos DB NoSQL data-plane API. For document operations, point your existing `CosmosClient` at Topaz's endpoint — the only changes are the account endpoint hostname and port, and the credential.

The emulator's default endpoint is `https://localhost:8081` with a fixed well-known key. Topaz uses a named account endpoint (`https://<account-name>.documents.topaz.local.dev:8895`) with a standard Azure credential. Update the endpoint and swap the credential for `AzureLocalCredential`.

Before migrating, verify that your application only uses the NoSQL API. MongoDB, Table, Gremlin, and Cassandra callers will need to stay on the emulator until Topaz adds those APIs. See the [roadmap](/roadmap) for planned coverage.
