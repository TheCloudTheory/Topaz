---
sidebar_position: 9
description: Topaz release roadmap — planned features and milestones across upcoming beta versions.
keywords: [topaz roadmap, azure emulator roadmap, planned features, upcoming releases]
---

# Roadmap

This page tracks what is planned for upcoming Topaz releases. The roadmap is derived from the [BACKLOG.md](https://github.com/TheCloudTheory/Topaz/blob/main/BACKLOG.md) file in the repository; each item there is automatically converted to a GitHub issue when committed.

:::info
The roadmap reflects current intentions and may change. Watch the [GitHub repository](https://github.com/TheCloudTheory/Topaz) or join [Discord](https://discord.gg/9eqCKe3N) to stay up to date.
:::

---

## v1.2-beta

### Queue Storage — preview

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Queue CRUD | Create, delete, list queues ✅ |
| <span class="badge--preview">Preview</span> | Service-level operations | List queues, get/set service properties, get stats |
| <span class="badge--preview">Preview</span> | Queue metadata & ACL | Get/set metadata and ACL per queue |
| <span class="badge--preview">Preview</span> | Message operations | Enqueue, dequeue, peek, update, delete, and clear messages |

### Key Vault — keys support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Core CRUD | Create, import, get, update, delete keys; list keys and versions |
| <span class="badge--stable">Stable</span> | Backup & Restore | Export and restore opaque key backup blobs |
| <span class="badge--stable">Stable</span> | Cryptographic operations | Encrypt, decrypt, sign, verify, wrap key, unwrap key |
| <span class="badge--preview">Preview</span> | Key rotation | Rotate key, get/update rotation policy |

### Azure PowerShell integration

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Certificate trust script | `configure-azure-powershell-cert.ps1` — trust the Topaz certificate in the Az module |
| <span class="badge--stable">Stable</span> | Cloud environment registration | `Add-AzEnvironment` + `Connect-AzAccount` setup script and example |
| <span class="badge--stable">Stable</span> | Test suite | `Topaz.Tests.AzurePowerShell` project with a Testcontainers fixture and smoke tests |

### Management Groups — basic CRUD

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Core CRUD | Create, update, get, delete, and list management groups |

### ARM Deployments — full support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Cancel | Mark an in-progress deployment as cancelled |
| <span class="badge--stable">Stable</span> | Export Template | Return the ARM template used for a deployment |
| <span class="badge--preview">Preview</span> | ~~What-If~~ | ~~Preview resource changes without applying them~~ |
| <span class="badge--stable">Stable</span> | List at all scopes | List deployments at subscription, management-group, and tenant scope |

### Packaging — CLI and Host split

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Separate CLI and Host artifacts | Split the monolithic binary into `topaz-host` (service process) and `topaz-cli` (thin client) — ⚠️ **Breaking change**: existing invocations and Docker image references must be updated |

---

## v1.3-beta

### Management Groups — extended operations

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Get Descendants | List all child management groups and subscriptions under a group |
| <span class="badge--preview">Preview</span> | Management Group Subscriptions | Associate, disassociate, and get subscriptions under a management group |
| <span class="badge--preview">Preview</span> | Hierarchy Settings | Create, update, get, list, and delete tenant-level hierarchy settings |
| <span class="badge--preview">Preview</span> | Entities list | `GET /providers/Microsoft.Management/getEntities` — list all accessible entities |

### Resource Providers — operations support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | List, Register, Unregister | Full provider lifecycle alongside the existing get-by-namespace operation |

### Virtual Networks — full control plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Delete, List, Update Tags | Complete the VNet control plane beyond create and get |
| <span class="badge--stable">Stable</span> | Check IP Address Availability | Validate whether an IP is available within a VNet's address space |
| <span class="badge--stable">Stable</span> | Subnets — full CRUD | Create, get, delete, and list subnets within a VNet |
| <span class="badge--preview">Preview</span> | Network Security Groups | Full NSG control plane: create, get, delete, list, update tags |

### Azure Virtual Machines — initial control plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | New service scaffold | `Topaz.Service.VirtualMachine` project with models, resource provider, and service registration |
| <span class="badge--preview">Preview</span> | Core control plane | Create/update, get, delete, list VMs — emulated only (no actual boot) |

### Key Vault — full certificate operations support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Core CRUD | Create, import, get, update, delete certificates; list certificates and versions |
| <span class="badge--preview">Preview</span> | Backup & Restore | Export and restore opaque certificate backup blobs |
| <span class="badge--preview">Preview</span> | Certificate contacts | Get, set, and delete the vault-level certificate administrator contacts |
| <span class="badge--preview">Preview</span> | Certificate issuers | Create, get, update, delete, and list certificate issuers |
| <span class="badge--preview">Preview</span> | Pending operations | Get, update, and cancel in-flight certificate creation operations |
| <span class="badge--preview">Preview</span> | Merge certificate | Merge a certificate from external PKI with a pending Key Vault CSR |
| <span class="badge--preview">Preview</span> | Soft-delete surface | List, get, recover, and purge deleted certificates |

### MCP Server — resource provisioning and tooling

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Resource provisioning tools | `CreateResourceGroup`, `CreateKeyVault`, `CreateServiceBusNamespace/Queue/Topic`, `CreateStorageAccount/BlobContainer` — thin wrappers over `TopazArmClient` following the `SubscriptionTool.cs` pattern |
| <span class="badge--preview">Preview</span> | Event Hub and Container Registry tools | `CreateEventHubNamespace`, `CreateEventHub`, `CreateContainerRegistry` |
| <span class="badge--preview">Preview</span> | `GetConnectionStrings` tool | Returns ready-to-use connection strings and URIs for all provisioned resources in a subscription — closes the provisioning workflow |
| <span class="badge--preview">Preview</span> | `GetTopazStatus` diagnostics tool | Wraps the Topaz health-check endpoint; returns running version, live services, and bound ports |
| <span class="badge--preview">Preview</span> | Pre-defined MCP prompts | Guided multi-step setup scenarios ("microservice environment", "CI bootstrap") that compose the provisioning tools into a single natural-language command |

---

## v1.4-beta

### Topaz Portal — tag editing

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Inline tag value editing in portal | Edit button per tag row in the Tags panel turns the value cell into an input field; supports all tag-capable resources |

### Key Vault — AES symmetric key (oct) cryptographic operations

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | AES encrypt/decrypt/wrap/unwrap for `oct` keys | Extend `KeyBundle` with the `k` field for raw key material; implement AES-GCM and AES-CBC(PAD) in the data plane so `az keyvault key encrypt/decrypt --algorithm A256GCM` (and SDK equivalents) work against symmetric keys |

### Key Vault — automated soft-delete purging

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Auto-purge soft-deleted vaults | Background scheduler permanently removes soft-deleted vaults once their `scheduledPurgeDate` has elapsed |
| <span class="badge--preview">Preview</span> | Auto-purge soft-deleted secrets | Background scheduler permanently removes soft-deleted secrets once their `scheduledPurgeDate` has elapsed |

### Storage Account — geo-replication semantics

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Secondary endpoint DNS & ARM response | Register `{accountName}-secondary.*` hostnames and populate `secondaryEndpoints` in the ARM response for RA-GRS/RA-GZRS accounts |
| <span class="badge--preview">Preview</span> | `GetServiceStats` on secondary endpoints | Return a realistic `GeoReplicationStats` payload (status: live, lastSyncTime: now) for Blob, Table, and Queue secondary endpoints |
| <span class="badge--preview">Preview</span> | `FeatureNotSupported` for non-RA-GRS stats | Return 403 on stats requests for LRS/ZRS accounts across all storage services (Table already done; extend to Blob and Queue) |
| <span class="badge--preview">Preview</span> | Read-only enforcement on secondary | Mutating operations (PUT, DELETE, POST) on secondary endpoints return 403 `WriteOperationNotSupportedOnSecondary` |

---

## v1.5-beta

### Container Registry — ACR Tasks

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Task CRUD control plane | Create, get, update, delete, and list ACR tasks via the ARM surface (`Microsoft.ContainerRegistry/registries/tasks`) |
| <span class="badge--preview">Preview</span> | Task runs and triggers | Manually trigger runs, list and get run details, cancel runs, retrieve log URL — runs complete immediately without executing real workloads |

### Azure SQL — initial control plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | New service scaffold | `Topaz.Service.Sql` project with models, resource provider, control plane, and host registration |
| <span class="badge--preview">Preview</span> | SQL Server control plane | Create, get, update, delete, and list SQL Server resources; emulated server with `{name}.database.topaz.local.dev` as FQDN |
| <span class="badge--preview">Preview</span> | SQL Database control plane | Create, get, update, delete, and list databases under a server — persisted as child resources on disk |

---

## v1.6-beta

### Azure Storage — unified data-plane port

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Unified storage port | Consolidate blob, table, queue, and file data-plane services onto a single HTTPS port with subdomain-based routing, matching real Azure's port topology and removing per-service port constants |

### ARM Deployments — mid-flight cancellation

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Cancel running deployments | Introduce cooperative cancellation into the orchestrator so that a cancel request against a `Running` deployment stops provisioning further resources after the current one completes, matching real Azure mid-flight cancellation semantics |

---

## ✅ Completed

### v1.1-beta

#### Key Vault — full secrets support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Get Secret Versions | `GET {vaultBaseUrl}/secrets/{name}/versions` — paged list of all versions for a secret |
| <span class="badge--stable">Stable</span> | Backup & Restore | `POST .../backup` and `POST .../restore` for opaque secret backup blobs |
| <span class="badge--stable">Stable</span> | Deleted secrets operations | Soft-delete surface: list, get, recover, and purge deleted secrets |

#### Container Registry — data plane preview

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | ACR OAuth2 token endpoint | `POST /oauth2/token` — completes the three-step ACR authentication flow |
| <span class="badge--preview">Preview</span> | Repositories & Tags | List repositories (`/v2/_catalog`), list tags, delete tag via manifest reference |
| <span class="badge--preview">Preview</span> | Manifest operations | GET, PUT, DELETE, HEAD for OCI manifests per registry |
| <span class="badge--preview">Preview</span> | Blob operations | Full OCI blob upload/download: start session, stream chunks, complete, download, delete |
