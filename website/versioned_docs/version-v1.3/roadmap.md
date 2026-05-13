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

## v1.4-beta

### Azure Storage — SAS validation and public access

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Account SAS query-string validation | Validate `?sv=…&sig=…` Account SAS tokens in Blob, Queue, and Table security providers; checks signature, expiry, service/resource-type/permission letters |
| <span class="badge--preview">Preview</span> | Service SAS query-string validation | Validate per-service SAS tokens (with per-service StringToSign for Blob, Queue, Table); includes `si=` stored-policy reference resolution |
| <span class="badge--preview">Preview</span> | Stored Access Policy enforcement | Look up named `<SignedIdentifier>` from Container/Queue/Table ACL XML at request time when `si=` is present; support policy revocation (403 when policy removed) |
| <span class="badge--preview">Preview</span> | Anonymous / public-access Blob reads | Allow unauthenticated GET/HEAD requests against containers created with `x-ms-blob-public-access: container` or `blob`; return the level in container property responses |

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

### Virtual Network — subnets and NICs

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Subnet CRUD | PUT/GET/DELETE/LIST subnets within a VNet (`/virtualNetworks/{vnetName}/subnets/{subnetName}`) |
| <span class="badge--preview">Preview</span> | Network Interface (NIC) CRUD | PUT/GET/DELETE/LIST network interfaces (`/networkInterfaces/{nicName}`) so `az vm create` can be used without manual ARM calls |

### Azure Storage — OData query support for Table Storage

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | `$filter` expression evaluation | Parse and evaluate OData filter expressions against entity properties (logical: `and`/`or`/`not`; comparison: `eq`/`ne`/`gt`/`ge`/`lt`/`le`; types: string, int32, int64, bool, datetime, guid) |
| <span class="badge--preview">Preview</span> | `$select` projection | Return only the requested property names in each entity |
| <span class="badge--preview">Preview</span> | `$top` page size | Limit the number of entities returned per response page |
| <span class="badge--preview">Preview</span> | `$skiptoken` continuation | Honour continuation tokens for server-side paging so multi-page SDK reads work correctly |

---

## v1.5-beta

### Azure Storage — User Delegation SAS for Blob

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | `generateUserDelegationKey` ARM endpoint | `POST .../storageAccounts/{name}/providers/Microsoft.Storage/userDelegationKey` — returns a time-bounded user delegation key signed with Topaz's account-key HMAC chain |
| <span class="badge--preview">Preview</span> | User Delegation SAS validation on Blob | Validate `skoid/sktid/skt/ske/sks/skv/sig` SAS query parameters on Blob endpoints; recompute the delegation key and verify signature, expiry, and scope |

### ARM Deployments — full tenant-scope surface

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Create Or Update At Tenant Scope | `PUT /providers/Microsoft.Resources/deployments/{name}` — deploy resources at tenant scope |
| <span class="badge--preview">Preview</span> | Get At Tenant Scope | `GET /providers/Microsoft.Resources/deployments/{name}` |
| <span class="badge--preview">Preview</span> | Delete At Tenant Scope | `DELETE /providers/Microsoft.Resources/deployments/{name}` |
| <span class="badge--preview">Preview</span> | Validate At Tenant Scope | `POST /providers/Microsoft.Resources/deployments/{name}/validate` |
| <span class="badge--preview">Preview</span> | Cancel At Tenant Scope | `POST /providers/Microsoft.Resources/deployments/{name}/cancel` |
| <span class="badge--preview">Preview</span> | Check Existence At Tenant Scope | `HEAD /providers/Microsoft.Resources/deployments/{name}` |
| <span class="badge--preview">Preview</span> | Export Template At Tenant Scope | `POST /providers/Microsoft.Resources/deployments/{name}/exportTemplate` |
| <span class="badge--preview">Preview</span> | What If At Tenant Scope | `POST /providers/Microsoft.Resources/deployments/{name}/whatif` |

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

### Virtual Network — IP address allocation registry

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | IP allocation registry | Track IPs assigned to NICs and private endpoints so `CheckIPAddressAvailability` can return real `availableIPAddresses` suggestions instead of `[]` |

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

### ACE (Azure Cost Estimator) integration

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Cost estimation backend endpoint | `GET /subscriptions/{sub}/providers/Microsoft.CostManagement/estimatedCosts` — uses [ACE](https://github.com/TheCloudTheory/arm-estimator) to return monthly cost estimates for all provisioned resources in a subscription; supports 17 currencies |
| <span class="badge--preview">Preview</span> | `topaz estimate` CLI command | New Topaz CLI sub-command that queries the Host's cost estimation endpoint and prints a formatted cost breakdown table; supports `--subscription`, `--currency`, and `--output` (table/json/csv) options |
| <span class="badge--preview">Preview</span> | Cost Analysis portal page | Dedicated **Cost Analysis** page in Topaz Portal showing per-resource-type estimated costs for the selected subscription, with currency selector and auto-refresh |

### Azure Cosmos DB — initial control plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | New service scaffold | `Topaz.Service.CosmosDb` project with models, resource provider, control plane, and host registration |
| <span class="badge--preview">Preview</span> | DatabaseAccount CRUD | Create, get, update, delete, list `Microsoft.DocumentDB/databaseAccounts`; emitted `documentEndpoint` follows `https://{name}.documents.topaz.local.dev:<port>/` |
| <span class="badge--preview">Preview</span> | Keys and connection strings | `listKeys`, `readonlykeys`, `regenerateKey`, and `listConnectionStrings` ARM actions; keys persisted and regeneratable |
| <span class="badge--preview">Preview</span> | SQL API — Database CRUD | Create, get, delete, list SQL databases and their throughput settings via `databaseAccounts/{name}/sqlDatabases` |
| <span class="badge--preview">Preview</span> | SQL API — Container CRUD | Create, get, update, delete, list SQL containers (with partitionKey, indexingPolicy, defaultTtl) and throughput settings via `sqlDatabases/{db}/containers` |

---

## v1.7-beta

### Azure Cosmos DB — SQL API data plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Data plane scaffold and master-key auth | Dedicated port; HMAC-SHA256 master-key signature validation (verb/resourceType/resourceLink/date StringToSign); 401 on invalid or expired signatures |
| <span class="badge--preview">Preview</span> | Database operations | `POST /dbs`, `GET /dbs/{db}`, `DELETE /dbs/{db}`, `GET /dbs` — full resource lifecycle with `_rid`, `_self`, `_etag`, `_ts` and `x-ms-request-charge` header |
| <span class="badge--preview">Preview</span> | Collection operations | `POST/GET/PUT/DELETE /dbs/{db}/colls/{coll}`, `GET /dbs/{db}/colls` — create, replace, and delete collections including indexingPolicy and partitionKey |
| <span class="badge--preview">Preview</span> | Document CRUD | `POST/GET/PUT/PATCH/DELETE /dbs/{db}/colls/{coll}/docs/{id}` — full item lifecycle with partition key enforcement, ETag optimistic concurrency (If-Match / 412), and JSON Patch partial updates |
| <span class="badge--preview">Preview</span> | SQL query execution | `POST /dbs/{db}/colls/{coll}/docs` with `x-ms-documentdb-isquery: true` — parameterised SQL subset: `SELECT`, `FROM`, `WHERE`, `ORDER BY`, `OFFSET/LIMIT`, aggregates (`COUNT`, `SUM`, `MIN`, `MAX`, `AVG`); continuation-token pagination |
| <span class="badge--preview">Preview</span> | MCP Server tools | `CreateCosmosDbAccount`, `CreateCosmosDbDatabase`, `CreateCosmosDbContainer`; `GetConnectionStrings` extended with Cosmos DB endpoint and key |

---

## ✅ Completed

### v1.3

_Released as v1.3.98 on 10 May 2026._

#### Management Groups — extended operations

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Get Descendants | List all child management groups and subscriptions under a group |
| <span class="badge--preview">Preview</span> | Management Group Subscriptions | Associate, disassociate, and get subscriptions under a management group |
| <span class="badge--preview">Preview</span> | Hierarchy Settings | Create, update, get, list, and delete tenant-level hierarchy settings |
| <span class="badge--preview">Preview</span> | Entities list | `GET /providers/Microsoft.Management/getEntities` — list all accessible entities |

#### Resource Providers — operations support

| | Feature | Description |
|--|---------|-------------|
| ✅ | List, Register, Unregister | Full provider lifecycle alongside the existing get-by-namespace operation. Registration state persisted per subscription and enforced in the router. |

#### Virtual Networks — full control plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Delete, List, Update Tags | Complete the VNet control plane beyond create and get |
| <span class="badge--stable">Stable</span> | Check IP Address Availability | Validate whether an IP is available within a VNet's address space |
| ✅ | Subnets — full CRUD | Create, get, delete, and list subnets within a VNet |
| <span class="badge--preview">Preview</span> | Network Security Groups | Full NSG control plane: create, get, delete, list, update tags |

#### Entra ID authentication for Azure Storage

| | Feature | Description |
|--|---------|-------------|
| ✅ | Entra ID bearer-token auth on Blob, Queue & Table data plane | Accept `Authorization: Bearer` tokens with full RBAC check; returns `401 + WWW-Authenticate` challenge when no Authorization header is present |
| ✅ | SharedKey HMAC for Blob & Queue (13-field format) | Blob and Queue now validate SharedKey signatures using the full 13-field Blob/Queue StringToSign (same algorithm as real Azure Storage) |
| ✅ | Consistent `Authorize` override pattern | Storage base classes override `IEndpointDefinition.Authorize` to bypass the Router's ARM RBAC check; per-request auth is handled in `IsRequestAuthorized` |

#### Azure Virtual Machines — initial control plane

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | New service scaffold | `Topaz.Service.VirtualMachine` project with models, resource provider, and service registration |
| <span class="badge--preview">Preview</span> | Core control plane | Create/update, get, delete, list VMs — emulated only (no actual boot) |

#### Key Vault — full certificate operations support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Core CRUD | Create, import, get, update, delete certificates; list certificates and versions |
| <span class="badge--preview">Preview</span> | Backup & Restore | Export and restore opaque certificate backup blobs |
| <span class="badge--preview">Preview</span> | Certificate contacts | Get, set, and delete the vault-level certificate administrator contacts |
| <span class="badge--preview">Preview</span> | Certificate issuers | Create, get, update, delete, and list certificate issuers |
| <span class="badge--preview">Preview</span> | Pending operations | Get, update, and cancel in-flight certificate creation operations |
| <span class="badge--preview">Preview</span> | Merge certificate | Merge a certificate from external PKI with a pending Key Vault CSR |
| <span class="badge--preview">Preview</span> | Soft-delete surface | List, get, recover, and purge deleted certificates |

#### MCP Server — resource provisioning and tooling

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Resource provisioning tools | `CreateResourceGroup`, `CreateKeyVault`, `CreateServiceBusNamespace/Queue/Topic`, `CreateStorageAccount/BlobContainer` — thin wrappers over `TopazArmClient` following the `SubscriptionTool.cs` pattern |
| <span class="badge--preview">Preview</span> | Event Hub and Container Registry tools | `CreateEventHubNamespace`, `CreateEventHub`, `CreateContainerRegistry` |
| <span class="badge--preview">Preview</span> | `GetConnectionStrings` tool | Returns ready-to-use connection strings and URIs for all provisioned resources in a subscription — closes the provisioning workflow |
| <span class="badge--preview">Preview</span> | `GetTopazStatus` diagnostics tool | Wraps the Topaz health-check endpoint; returns running version, live services, and bound ports |
| <span class="badge--preview">Preview</span> | Pre-defined MCP prompts | Guided multi-step setup scenarios ("microservice environment", "CI bootstrap") that compose the provisioning tools into a single natural-language command |

### v1.2

#### Queue Storage

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Queue CRUD | Create, delete, list queues ✅ |
| <span class="badge--stable">Stable</span> | Message operations | Enqueue, dequeue, peek, update, delete ✅ |
| <span class="badge--stable">Stable</span> | Queue metadata & ACL | Get/set metadata and ACL per queue ✅ |
| <span class="badge--stable">Stable</span> | Service-level operations | Get/set service properties, get stats ✅ |

#### Key Vault — keys support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Core CRUD | Create, import, get, update, delete keys; list keys and versions |
| <span class="badge--stable">Stable</span> | Backup & Restore | Export and restore opaque key backup blobs |
| <span class="badge--stable">Stable</span> | Cryptographic operations | Encrypt, decrypt, sign, verify, wrap key, unwrap key, release |
| <span class="badge--preview">Preview</span> | Key rotation | Rotate key, get/update rotation policy |

#### Azure PowerShell integration

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Certificate trust script | `configure-azure-powershell-cert.ps1` — trust the Topaz certificate in the Az module |
| <span class="badge--stable">Stable</span> | Cloud environment registration | `Add-AzEnvironment` + `Connect-AzAccount` setup script and example |
| <span class="badge--stable">Stable</span> | Test suite | `Topaz.Tests.AzurePowerShell` project with a Testcontainers fixture and smoke tests |

#### Management Groups — basic CRUD

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Core CRUD | Create, update, get, delete, and list management groups |

#### ARM Deployments — full support

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--stable">Stable</span> | Cancel | Mark an in-progress deployment as cancelled |
| <span class="badge--stable">Stable</span> | Export Template | Return the ARM template used for a deployment |
| <span class="badge--preview">Preview</span> | ~~What-If~~ | ~~Preview resource changes without applying them~~ |
| <span class="badge--stable">Stable</span> | List at all scopes | List deployments at subscription, management-group, and tenant scope ✅ |

#### Packaging — CLI and Host split

| | Feature | Description |
|--|---------|-------------|
| <span class="badge--preview">Preview</span> | Separate CLI and Host artifacts | Split the monolithic binary into `topaz-host` (service process) and `topaz-cli` (thin client) — ⚠️ **Breaking change**: existing invocations and Docker image references must be updated |

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
