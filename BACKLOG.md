# Backlog

Central place for planning upcoming work. Each TODO block below is automatically
converted to a GitHub Issue by CI when new lines are committed.

> **Note:** The action only picks up TODOs that appear in the *diff* of a new commit.
> To bulk-import existing items, run the _TODO to Issue_ workflow manually from the
> **Actions** tab.

---

<!--
TODO: VirtualNetwork — Network Interface (NIC) CRUD
  Implement PUT/GET/DELETE/LIST NIC endpoints under /networkInterfaces/{nicName}.
  Required to support `az vm create` (which creates a NIC and wires it into the VM payload).
  milestone: v1.5-beta
  labels: enhancement, good first issue
-->

---

## Format reference

```
<!-- 
T0DO: Short issue title (required)
  Longer description that becomes the issue body. (optional)
  milestone: v1.0          ← must match a GitHub milestone name (created if missing)
  labels: enhancement      ← comma-separated; created if missing
  assignees: github-handle ← comma-separated GitHub usernames
-->
```

---

## v1.1-beta

### Key Vault — full secrets support

_Implemented in v1.1-beta: `GetVersions`, backup/restore, and full soft-delete surface (`ListDeleted`, `GetDeleted`, `Recover`, `Purge`) for secrets._

### Container Registry — data plane preview

_Implemented in v1.1-beta: OAuth2 token exchange, repository/tag listing (`/v2/_catalog`, `/v2/{name}/tags/list`), manifest CRUD (`GET/PUT/DELETE/HEAD`), and full blob upload/download pipeline (`GET/HEAD/DELETE`, chunked upload via `POST/PATCH/PUT`)._

---

## v1.2-beta

### Key Vault — keys support

_Implemented in v1.2-beta: core key CRUD (create, import, get, update, delete, list, list versions), backup/restore, key rotation, and rotation policy._

### Azure PowerShell integration

_Implemented in v1.2-beta: `configure-azure-powershell-cert.ps1`, `configure-azure-powershell-env.ps1`, and `Topaz.Tests.AzurePowerShell` test project (PowerShellTestBase fixture, resource group, and Key Vault tests)._

### Management Groups — basic CRUD

_Implemented in v1.2-beta: `PUT/GET/DELETE/PATCH` management groups and `GET /providers/Microsoft.Management/managementGroups` list._

### ARM Deployments — full support

_Implemented in v1.2-beta: `cancel` and `exportTemplate` at subscription scope; list at subscription, management-group, and tenant scope._

### Packaging — CLI and Host split

_Implemented in v1.2-beta: `topaz-host` and `topaz-cli` published as independent executables and Docker images._

---

## v1.3-beta

### Management Groups — extended operations

_Implemented in v1.3-beta: Get Descendants, Management Group Subscriptions (associate/disassociate/get), Hierarchy Settings (create/update/get/list/delete), and Entities list._

### Resource Providers — operations support

Implemented: `GET /subscriptions/{sub}/providers`, `POST .../register`, `POST .../unregister`. Registration state is persisted per subscription and enforced cross-cutting in the Router (returns 409 `MissingSubscriptionRegistration` for unregistered namespaces).

### Entra ID authentication for Azure Storage

_Implemented: Bearer token (Entra ID) authentication added to Blob, Queue, and Table Storage data-plane endpoints alongside the existing SharedKey/SharedKeyLite mechanism. Full RBAC check via `AzureAuthorizationAdapter.PrincipalHasPermissions`. Returns `401 + WWW-Authenticate` challenge when no Authorization header is present. SharedKey HMAC validation for Blob/Queue now uses the 13-field Blob/Queue StringToSign format. `IEndpointDefinition.Authorize` override in storage base classes bypasses the Router's ARM RBAC check; auth is managed per-request in `IsRequestAuthorized`._

### Azure Virtual Machines — initial control plane

_Implemented in v1.3-beta: service scaffold, five control-plane endpoints (create/update, get, delete, list by resource group, list by subscription), E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests._

### Key Vault — full certificate operations support

_Implemented in v1.3-beta: core CRUD (create, import, get, update, delete, list, list versions), backup/restore, certificate contacts, certificate issuers, pending operations, merge certificate, and soft-delete surface._

### MCP Server — resource provisioning and tooling

_Implemented in v1.3-beta: resource provisioning tools (Key Vault, Service Bus, Storage, Event Hub, Container Registry), `GetConnectionStrings`, `GetTopazStatus` diagnostics, and pre-defined MCP prompts._

---

## v1.4-beta

### Azure Storage — SAS (Shared Access Signature) validation

_Implemented in v1.4-beta: Account SAS and Service SAS query-string validation for Blob, Queue, and Table data-plane endpoints. Stored access policy enforcement (`si=`) with revocation support across all three services._

### Topaz Portal — tag editing

_Implemented in v1.4-beta: inline per-row editing added to the shared `TagsPanel` component (moved to `Components/Shared/`). Each tag row gains Edit / Save / Cancel controls; saving calls the existing `CreateOrUpdate*Tag` upsert. All 9 taggable-resource pages (Subscription, Resource Group, Key Vault, Storage, Managed Identity, Event Hub, Service Bus, Virtual Machine, Virtual Network) are wired. Also fixed a duplicate-key bug in `CreateOrUpdateSubscriptionTag`._

### Key Vault — automated soft-delete purging

### Storage Account — geo-replication semantics

_Implemented in v1.4-beta: secondary endpoint DNS registration, ARM response `secondaryEndpoints` for RA-GRS/RA-GZRS, `GET ?restype=service&comp=stats` on blob/table/queue secondary endpoints, 403 FeatureNotSupported for non-RA-GRS stats, 403 WriteOperationNotSupportedOnSecondary for mutations on secondary._

<!--
TODO: Storage Account — secondary endpoint general reads
  Allow standard GET operations (blob download, container listing, queue listing, table
  entity reads) on `{accountName}-secondary.*` endpoints for RA-GRS/RA-GZRS accounts,
  returning data from the same in-memory store as the primary endpoint.
  milestone: v1.6-beta
  labels: enhancement, storage
-->

### Virtual Networks — full control plane

_Implemented in v1.4-beta: `GET .../virtualNetworks/{name}/checkIPAddressAvailability?ipAddress={ip}`. Returns `available: true` when the IP falls within any subnet CIDR, `false` otherwise. `availableIPAddresses` is always `[]` (IP allocation tracking planned for v1.5-beta — see known-limitations.md)._

_Implemented: DELETE, List by resource group, List by subscription, and Update Tags operations._



---

## v1.5-beta

### Azure Storage — User Delegation SAS for Blob

<!--
TODO: Storage: User Delegation SAS for Blob data-plane
  User Delegation SAS tokens are signed with a time-bounded user delegation key derived
  from an Entra ID identity rather than the storage account key. They are scoped to Blob
  only. Implementation requires two coordinated pieces:

  1. ARM endpoint — POST generateUserDelegationKey:
     Add `POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Storage/
     storageAccounts/{name}/providers/Microsoft.Storage/userDelegationKey` (or the
     direct ARM route used by the SDK) that accepts a JSON body with `keyInfo.start`
     and `keyInfo.expiry` and returns a `StorageServiceProperties`-style response
     with the fields `signedOid`, `signedTid`, `signedStart`, `signedExpiry`,
     `signedService` ("b"), `signedVersion`, and `value` (base64 key bytes). Topaz
     can derive the key bytes deterministically from the account key + signed fields
     via HMAC-SHA256.

  2. Blob data-plane validation:
     Detect User Delegation SAS query parameters (sv, sr, sp, se, st, spr, sip,
     skoid, sktid, skt, ske, sks, skv, sig) and validate them in
     BlobStorageSecurityProvider:
     - Recompute the user delegation key from the stored account key + (skoid, sktid,
       skt, ske, sks, skv) fields using the same HMAC chain used during issuance.
     - Build the StringToSign (identical to Service SAS but uses the user delegation
       key bytes rather than the raw account key).
     - Validate signature, expiry, IP/protocol restrictions, and scope (sr=b/c/d).
  milestone: v1.5-beta
  labels: enhancement, storage, security
-->

### ARM Deployments — full tenant-scope surface

<!--
TODO: ARM Deployments: Full tenant-scope surface
  Implement all remaining tenant-scope deployment operations:
  - PUT    /providers/Microsoft.Resources/deployments/{name}                       – create or update
  - GET    /providers/Microsoft.Resources/deployments/{name}                       – get
  - DELETE /providers/Microsoft.Resources/deployments/{name}                       – delete
  - POST   /providers/Microsoft.Resources/deployments/{name}/validate              – validate
  - POST   /providers/Microsoft.Resources/deployments/{name}/cancel                – cancel
  - HEAD   /providers/Microsoft.Resources/deployments/{name}                       – check existence
  - POST   /providers/Microsoft.Resources/deployments/{name}/exportTemplate        – export template
  - POST   /providers/Microsoft.Resources/deployments/{name}/whatif               – what-if
  List at tenant scope is already implemented (v1.2). All other operations follow the
  subscription-scope pattern but without a subscriptionId segment in the path.
  milestone: v1.5-beta
  labels: enhancement, resource-manager, good first issue
-->

### Container Registry — ACR Tasks

<!--
TODO: ACR Tasks: Task CRUD control plane
  Implement the ARM-level task management surface under the ACR Tasks API (2019-04-01):
  - PUT    .../registries/{registry}/tasks/{task}  – create or update task
  - GET    .../registries/{registry}/tasks/{task}  – get task
  - DELETE .../registries/{registry}/tasks/{task}  – delete task
  - GET    .../registries/{registry}/tasks          – list tasks
  - PATCH  .../registries/{registry}/tasks/{task}  – update task
  Follow the existing ContainerRegistry service conventions. Add TaskResourceProperties
  and TaskResource (ArmResource<T>) with fields matching the Azure REST API schema:
  https://learn.microsoft.com/en-us/rest/api/container-registry-tasks/tasks?view=rest-container-registry-tasks-2019-04-01
  milestone: v1.5-beta
  labels: enhancement, container-registry, good first issue
-->

<!--
TODO: ACR Tasks: Task Run CRUD and trigger operations
  Implement the run lifecycle endpoints:
  - POST   .../registries/{registry}/tasks/{task}/run        – manually trigger a task run
  - GET    .../registries/{registry}/runs/{run}              – get run details
  - PATCH  .../registries/{registry}/runs/{run}              – update run (cancel)
  - GET    .../registries/{registry}/runs                    – list runs
  - POST   .../registries/{registry}/runs/{run}/listLogSasUrl – get log SAS URL
  - POST   .../registries/{registry}/scheduleRun             – schedule an ad-hoc run
  Runs do not execute real container workloads; report provisioningState as Succeeded
  immediately and return a static log body from the log SAS URL endpoint.
  milestone: v1.5-beta
  labels: enhancement, container-registry
-->

### Azure SQL — initial control plane

<!--
TODO: Azure SQL: New service project scaffold
  Create Topaz.Service.Sql following the existing service conventions:
  - Project file with references to Topaz.ResourceManager and Topaz.Service.Shared
  - SqlServerResourceProperties + SqlServerResource (ArmResource<T>)
  - SqlServerResourceProvider (ResourceProviderBase<T>) for filesystem persistence
  - SqlServiceControlPlane implementing IControlPlane with Deploy()
  - IServiceDefinition registration and wiring in Topaz.Host
  - ProjectReference in Topaz.Service.ResourceManager for template deployment routing
  milestone: v1.5-beta
  labels: enhancement, azure-sql, good first issue
-->

<!--
TODO: Azure SQL: Server control plane endpoints
  Implement the ARM-level SQL Server resource surface:
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}  – delete
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Sql/servers                              – list all
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}  – update
  The emulated server does not run a real SQL engine. provisioningState is reported
  as Succeeded immediately. fullyQualifiedDomainName follows the pattern
  {name}.database.topaz.local.dev.
  milestone: v1.5-beta
  labels: enhancement, azure-sql, good first issue
-->

<!--
TODO: Azure SQL: Database control plane endpoints
  Implement the ARM-level SQL Database resource surface under a server:
  - PUT    .../servers/{server}/databases/{database}  – create or update
  - GET    .../servers/{server}/databases/{database}  – get
  - DELETE .../servers/{server}/databases/{database}  – delete
  - GET    .../servers/{server}/databases              – list
  - PATCH  .../servers/{server}/databases/{database}  – update
  Databases are persisted as child resources of their server via the resource provider.
  milestone: v1.5-beta
  labels: enhancement, azure-sql, good first issue
-->

### Virtual Network — IP address allocation registry

<!--
TODO: Virtual Networks: IP address allocation registry
  Introduce an IP allocation registry in the VNet service so that NIC and private endpoint
  creation records the assigned IP address. This enables:
  - Real `availableIPAddresses` computation in the CheckIPAddressAvailability endpoint
    (currently always returns `[]` — see known-limitations.md).
  - More accurate `available: false` responses when an IP within a subnet is already
    assigned to a resource.
  milestone: v1.5-beta
  labels: enhancement, virtual-network
-->

### Azure App Service — initial control plane

<!--
TODO: Azure App Service: Site Config sub-resource endpoints
  Implement the /config sub-resource surface for Microsoft.Web/sites:
  - GET  .../sites/{name}/config/web              – return siteConfig wrapped as
    {"id": ".../config/web", "name": "web", "type": "Microsoft.Web/sites/config",
     "properties": {<full SiteConfigProperties>}}
  - PUT  .../sites/{name}/config/web              – merge request.properties into the stored
    siteConfig and return 200 with the same envelope
  - PUT  .../sites/{name}/config/appsettings       – replace the appSettings name/value list;
    return 200 with {"id": ".../config/appsettings", "name": "appsettings",
    "type": "Microsoft.Web/sites/config", "properties": {"KEY": "VALUE", ...}}
  - POST .../sites/{name}/config/appsettings/list  – return the current app settings dictionary
    in the same envelope (used by Azure CLI az webapp config appsettings list)
  siteConfig is stored embedded in AppServiceSiteResourceProperties (same persistence file as
  the site resource). No separate subresource file is needed for initial support.
  milestone: v1.5-beta
  labels: enhancement, app-service
-->

### Event Hub — delete individual Event Hub

<!--
TODO: Event Hub: Delete individual Event Hub endpoint
  Implement DELETE .../namespaces/{namespaceName}/eventhubs/{eventhubName}.
  The Azure SDK EventHubsClient.delete() and Azure CLI az eventhubs eventhub delete
  expect a 200 response when removing an individual Event Hub resource inside a namespace.
  Currently only namespace deletion is supported; deleting a hub returns 404, forcing
  clients to skip cleanup and leave orphaned resources.
  Follow the one-file-per-operation convention under Services/Topaz.Service.EventHub/Endpoints/.
  milestone: v1.5-beta
  labels: enhancement, event-hub, good first issue
-->

### Virtual Machines — PATCH update endpoint

<!--
TODO: Virtual Machines: PATCH update endpoint
  Implement PATCH .../virtualMachines/{vmName} to support partial updates such as
  tag changes and hardware-profile modifications without requiring the caller to
  supply a full VM body.
  The Azure SDK VirtualMachinesClient.begin_update() (and the equivalent in .NET and
  the Azure CLI az vm update) uses PATCH. Without this endpoint, clients must fall
  back to a full PUT CreateOrUpdate, which requires re-supplying the OS profile,
  storage profile, and NIC references.
  Follow the one-file-per-operation convention under Services/Topaz.Service.VirtualMachine/Endpoints/.
  milestone: v1.5-beta
  labels: enhancement, virtual-machine, good first issue
-->

---

## v1.6-beta

### Key Vault — correct challenge resource in `WWW-Authenticate`

<!--
TODO: Key Vault: Return accurate challenge resource in WWW-Authenticate
  Topaz currently returns resource="https://vault.azure.net" in the WWW-Authenticate
  header on 401 responses from Key Vault endpoints. Non-.NET Azure SDK clients (Python,
  JavaScript, etc.) verify that this resource matches the domain used to reach the vault.
  Because Topaz is accessed at a custom domain (e.g. pytest-kv.vault.topaz.local.dev),
  the mismatch causes those clients to reject the challenge and raise an exception before
  issuing any authenticated request.

  Fix: replace the hard-coded "https://vault.azure.net" with the vault's actual request
  URL (derived from the incoming request's Host header). This removes the domain mismatch
  so clients that verify the challenge resource work without any workaround.

  See also: website/docs/known-limitations.md — "Key Vault — WWW-Authenticate challenge
  resource does not match emulator domain".
  milestone: v1.6-beta
  labels: enhancement, key-vault
-->

### AMQP — spec-compliant performative encoding for non-.NET clients

<!--
TODO: AMQP: Fix AMQPNetLite protocol deviations that break non-.NET clients
  AMQPNetLite 2.5.1 exhibits two spec deviations that prevent non-.NET AMQP clients
  (Python azure-servicebus, Go, JavaScript) from working without client-side patches:

  1. Trailing null fields are omitted in encoded performatives (Attach, Transfer, Flow,
     Disposition, etc.). The AMQP 1.0 spec allows this, but several clients access fields
     by fixed numeric index and raise IndexError / panic when the frame is shorter than
     expected.

  2. Error composites are encoded with two fields [condition, description] instead of
     three [condition, description, info]. Clients that unconditionally access error[2]
     crash when receiving any Detach or Close frame that carries an error.

  Investigation required:
  - Check whether a newer version of AMQPNetLite (> 2.5.1) has resolved both issues.
  - If not, evaluate replacing AMQPNetLite with a fully spec-compliant server
    implementation (e.g. Apache Qpid Proton .NET or a custom minimal AMQP 1.0 server).
  - Validate that the replacement works with .NET, Python, JavaScript, and Go Azure SDK
    clients for Service Bus and Event Hubs without any client-side frame-padding patches.

  See also: website/docs/known-limitations.md — "AMQP — AMQPNetLite protocol deviations
  break non-.NET clients".
  milestone: v1.6-beta
  labels: enhancement, service-bus, event-hub, amqp
-->

### ARM Deployments — mid-flight cancellation

<!--
TODO: ARM Deployments: Cancel running deployments mid-flight
  The current orchestrator processes deployments on a single background thread with no
  interruption mechanism. A cancel request against a Running deployment returns 409 Conflict.
  Introduce cooperative cancellation so that:
  - A CancellationTokenSource is created per deployment when it is dequeued.
  - CancelDeployment signals the token when the deployment is Running.
  - RouteDeployment checks the token between resource provisions and stops early if signalled,
    leaving already-provisioned resources in place.
  - The deployment's provisioningState transitions to Canceled after the current resource
    completes, matching real Azure mid-flight cancellation semantics.
  See also: website/docs/known-limitations.md — "ARM Deployments — running deployments cannot be cancelled".
  milestone: v1.6-beta
  labels: enhancement, resource-manager
-->

### Azure Storage — unified data-plane port

<!--
TODO: Storage: Consolidate all data-plane services onto a single HTTPS port
  Real Azure exposes all storage sub-services (blob, table, queue, file) on port 443 with
  subdomain-based routing ({account}.blob.core.windows.net, etc.).  Topaz currently uses
  separate HTTP ports per sub-service (blob=8891, table=8890, queue=8893, file=8894).
  This causes problems for Azure CLI/SDK code paths that construct storage URLs via
  get_account_url() — the function builds a single https:// URL from the cloud-suffix
  `storage_endpoint`, which can only encode one port, so blob/table/queue/file end up on
  the wrong port or wrong scheme.

  Work required:
  - Add subdomain-aware filtering to the Router (filter by Host header prefix before port match,
    or let all storage endpoints share one port and disambiguate by subdomain in the router).
  - Consolidate DefaultBlobStoragePort / DefaultTableStoragePort / DefaultQueueStoragePort /
    DefaultFileStoragePort into a single DefaultStoragePort constant.
  - Update all PortsAndProtocol declarations in Endpoints/Blob, Endpoints/Table, Endpoints/Queue,
    Endpoints/File to use the unified port and Protocol.Https.
  - Update BuildPrimaryEndpoints in AzureStorageControlPlane to emit https:// URLs on the
    single port for all sub-services.
  - Update the `storage` suffix in GetMetadataEndpointResponse.Suffixes to reflect the new
    host:port, and re-evaluate the Terraform ParseAccountID suffix workaround.
  - Update TopazResourceHelpers connection-string builder.
  - Update all tests (E2E, AzureCLI, Terraform) that reference explicit storage ports.
  See also: website/docs/known-limitations.md — "Azure Storage — per-service ports".
  milestone: v1.6-beta
  labels: enhancement, storage
-->

### ACE (Azure Cost Estimator) integration

<!--
TODO: ACE Integration: Cost estimation backend endpoint in Topaz Host
  Integrate ACE (https://github.com/TheCloudTheory/arm-estimator) as a library into
  Topaz.Host to expose a cost analysis REST endpoint:
  - GET /subscriptions/{sub}/providers/Microsoft.CostManagement/estimatedCosts
    Returns cost estimates for all provisioned resources in a subscription, grouped by
    resource type, with monthly cost totals. Accepts an optional `currency` query
    parameter (default: USD) mapping to the 17 currencies ACE supports.
  Wiring:
  - Add ACE project reference (or NuGet package) to Topaz.Host.
  - Walk the subscription's persisted resource files via the existing resource providers.
  - Build ACE-compatible resource descriptors from the stored ArmResource<T> models and
    call the ACE estimation engine.
  - Return the result as JSON so both the CLI command and the Portal page can consume it.
  milestone: v1.6-beta
  labels: enhancement, ace, finops
-->

<!--
TODO: ACE Integration: `topaz estimate` CLI command
  Add a `topaz estimate` sub-command to Topaz.CLI that queries the running Host's cost
  estimation endpoint and prints a formatted cost breakdown table to stdout.
  Options:
  - `--subscription <id>`  — target subscription (default: first available)
  - `--currency <code>`    — ISO 4217 currency code, e.g. USD, EUR, GBP (default: USD)
  - `--output <format>`    — table (default) | json | csv
  The command follows the same pattern as existing Topaz.CLI commands (e.g. StartCommand).
  milestone: v1.6-beta
  labels: enhancement, ace, finops, cli
-->

<!--
TODO: ACE Integration: Cost Analysis page in Topaz Portal
  Add a dedicated "Cost Analysis" page to Topaz.Portal that shows ACE-powered estimated
  monthly costs for all provisioned resources in the selected subscription.
  UI requirements:
  - A top-level nav entry "Cost Analysis" linking to /cost-analysis.
  - Subscription selector (reuse the existing pattern from other portal pages).
  - A table listing each resource type with its individual and cumulative estimated cost.
  - A total monthly estimate at the bottom.
  - A currency selector (dropdown of the 17 currencies ACE supports).
  - Auto-refresh when the subscription or currency changes.
  The page calls the Host's GET estimatedCosts endpoint via ITopazClient.
  Include a bUnit component test in Topaz.Tests.Portal/ covering the render and
  data-load behaviour (mock ITopazClient response).
  milestone: v1.6-beta
  labels: enhancement, ace, finops, portal
-->

### Azure Cosmos DB — initial control plane

<!--
TODO: Azure Cosmos DB: New service project scaffold
  Create Topaz.Service.CosmosDb following existing service conventions:
  - Project file with references to Topaz.ResourceManager and Topaz.Service.Shared
  - DatabaseAccountResourceProperties + DatabaseAccountResource (ArmResource<T>) capturing
    the full ARM body: kind, consistencyPolicy, locations, databaseAccountOfferType,
    ipRules, isVirtualNetworkFilterEnabled, enableAutomaticFailover, capabilities,
    publicNetworkAccess, enableFreeTier, enableAnalyticalStorage, apiProperties.
  - DatabaseAccountResourceProvider (ResourceProviderBase<T>) for filesystem persistence
    under .topaz/cosmos-db/{subscriptionId}/{resourceGroup}/{accountName}/.
  - CosmosDbServiceControlPlane implementing IControlPlane with a working Deploy()
    that maps GenericResource → DatabaseAccountResource via resource.As<T,TProps>().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "Microsoft.DocumentDB/databaseAccounts": entry in TemplateDeploymentOrchestrator.RouteDeployment().
  milestone: v1.6-beta
  labels: enhancement, cosmos-db, good first issue
-->

<!--
TODO: Azure Cosmos DB: DatabaseAccount control plane endpoints
  Implement the ARM-level DatabaseAccount resource surface (Microsoft.DocumentDB/databaseAccounts):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}  – update (tags, consistencyPolicy)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.DocumentDB/databaseAccounts                              – list all
  The emulated account does not run a real Cosmos DB engine. provisioningState is reported
  as Succeeded immediately. documentEndpoint follows the pattern
  https://{name}.documents.topaz.local.dev:<CosmosDbPort>/.
  AccountKind defaults to GlobalDocumentDB (SQL API). Generate and persist 2 read-write
  and 2 read-only master keys (base64url-encoded random 64-byte blobs) on first creation.
  milestone: v1.6-beta
  labels: enhancement, cosmos-db, good first issue
-->

<!--
TODO: Azure Cosmos DB: Key and connection string management endpoints
  Implement the ARM-level key/connection-string surface:
  - POST   .../databaseAccounts/{name}/listKeys               – return primaryMasterKey, secondaryMasterKey, primaryReadonlyMasterKey, secondaryReadonlyMasterKey
  - POST   .../databaseAccounts/{name}/readonlykeys           – return read-only keys only
  - POST   .../databaseAccounts/{name}/regenerateKey          – regenerate one of the four keys by keyKind (primary|secondary|primaryReadonly|secondaryReadonly)
  - POST   .../databaseAccounts/{name}/listConnectionStrings  – return AccountEndpoint=...;AccountKey=... connection strings for both primary and secondary keys
  Keys are persisted in the DatabaseAccountResourceProperties and updated by regenerateKey.
  milestone: v1.6-beta
  labels: enhancement, cosmos-db, good first issue
-->

<!--
TODO: Azure Cosmos DB: SQL API — Database control plane endpoints
  Implement the ARM-level SQL database resource surface:
  - PUT    .../databaseAccounts/{name}/sqlDatabases/{database}                           – create or update (body: resource.id, options.throughput / options.autoscaleSettings)
  - GET    .../databaseAccounts/{name}/sqlDatabases/{database}                           – get (returns resource including _rid, _self, _etag, _colls, _users)
  - DELETE .../databaseAccounts/{name}/sqlDatabases/{database}                           – delete
  - GET    .../databaseAccounts/{name}/sqlDatabases                                      – list
  - GET    .../databaseAccounts/{name}/sqlDatabases/{database}/throughputSettings/default – get throughput (RU/s or autoscale)
  - PUT    .../databaseAccounts/{name}/sqlDatabases/{database}/throughputSettings/default – update throughput
  Persist SQL databases as subresources under the account directory.
  milestone: v1.6-beta
  labels: enhancement, cosmos-db, good first issue
-->

<!--
TODO: Azure Cosmos DB: SQL API — Container control plane endpoints
  Implement the ARM-level SQL container resource surface:
  - PUT    .../sqlDatabases/{database}/containers/{container}                            – create or update (body: resource.id, resource.partitionKey, resource.indexingPolicy, resource.uniqueKeyPolicy, resource.defaultTtl, options.throughput)
  - GET    .../sqlDatabases/{database}/containers/{container}                            – get
  - DELETE .../sqlDatabases/{database}/containers/{container}                            – delete
  - GET    .../sqlDatabases/{database}/containers                                        – list
  - GET    .../sqlDatabases/{database}/containers/{container}/throughputSettings/default – get throughput
  - PUT    .../sqlDatabases/{database}/containers/{container}/throughputSettings/default – update throughput
  Persist containers as child subresources of their SQL database.
  Store the partitionKey path, indexingPolicy, uniqueKeyPolicy, and defaultTtl.
  milestone: v1.6-beta
  labels: enhancement, cosmos-db, good first issue
-->

### Azure Disks — initial control plane

<!--
TODO: Azure Disks: New service project scaffold
  Create Topaz.Service.Disk following the existing service conventions:
  - DiskResourceProperties + DiskResource (ArmResource<T>) with fields: diskSizeGB,
    diskSizeBytes, diskIOPSReadWrite, diskMBpsReadWrite, osType, hyperVGeneration,
    creationData (createOption, sourceResourceId, imageReference), diskState,
    provisioningState (always Succeeded), timeCreated, uniqueId (GUID).
  - DiskResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - DiskServiceControlPlane implementing IControlPlane with a working Deploy().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj.
  - case "Microsoft.Compute/disks" in TemplateDeploymentOrchestrator.RouteDeployment().
  See: https://learn.microsoft.com/en-us/rest/api/compute/disks?view=rest-compute-2025-11-01
  milestone: v1.6-beta
  labels: enhancement, good first issue
-->

<!--
TODO: Azure Disks: Managed Disk control plane endpoints
  Implement the ARM-level Disk resource surface (Microsoft.Compute/disks):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}  – update (tags, diskSizeGB, SKU)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Compute/disks                              – list all in subscription
  Follow the one-file-per-operation convention under Endpoints/. Return 200 for get/update,
  201 for create, 204 for delete. provisioningState is always Succeeded.
  milestone: v1.6-beta
  labels: enhancement
-->

<!--
TODO: Azure Disks: SAS access endpoints
  Implement the SAS URI access operations:
  - POST .../disks/{name}/beginGetAccess  – grant read or readwrite access, returning an accessSAS URI stub
  - POST .../disks/{name}/endGetAccess    – revoke access (returns 200 with empty body)
  The SAS URI value is a stub (no actual data streaming required); return a plausible
  https://md-{uniqueId}.blob.core.windows.net/... URL so SDK/CLI callers do not error.
  milestone: v1.6-beta
  labels: enhancement
-->

---

## v1.7-beta

### Storage — Service SAS permission enforcement

<!--
TODO: Storage: Service SAS permission-letter enforcement (sp → HTTP verb mapping)
  Map the `sp` permission letters in a Service SAS token to the HTTP method of the
  incoming request (e.g. r=GET, w=PUT, d=DELETE, l=GET+comp=list, a=POST messages,
  p=GET+dequeue, u=PUT messages, c=create). Return 403 AuthorizationPermissionMismatch
  when the method is not covered by the token's permissions.
  Applies to all three storage services: Blob, Queue, and Table.
  milestone: v1.7-beta
  labels: enhancement, storage, security
-->

### Azure Cosmos DB — SQL API data plane

<!--
TODO: Azure Cosmos DB: Data plane service scaffold and authentication
  Add a CosmosDbDataPlane service (analogous to AcrDataPlane) that handles the
  Cosmos DB REST API (https://learn.microsoft.com/en-us/rest/api/cosmos-db/) on a
  dedicated port (GlobalSettings.DefaultCosmosDbPort, 8894).
  Authentication:
  - Implement master-key HMAC-SHA256 signature validation.
    StringToSign format: verb.toLowerCase() + "\n" + resourceType.toLowerCase() + "\n"
      + resourceLink + "\n" + date.toLowerCase() + "\n" + "" + "\n"
    (all values lowercased; trailing newlines are significant.)
  - Parse the Authorization header: type=master&ver=1.0&sig=<base64>
  - Derive the signing key from the stored primaryMasterKey bytes (base64-decode first).
  - Validate that the computed HMAC matches sig=, and that x-ms-date is within 15 minutes
    of the server clock (replay-attack window matching real Azure).
  - Return 401 with a structured JSON error body on auth failure.
  All data-plane endpoints must inherit from a CosmosDataPlaneEndpointBase that
  calls IsRequestAuthorized and returns 401 before processing.
  milestone: v1.7-beta
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: Data plane — Database operations
  Implement the Cosmos DB REST API database resource endpoints:
  - POST   /{dbs}                    – create database (body: {"id": "<name>"}, optional x-ms-offer-throughput header)
  - GET    /{dbs}/{db}               – get database (returns _rid, _self, _etag, _colls, _users, _ts)
  - DELETE /{dbs}/{db}               – delete database
  - GET    /{dbs}                    – list databases (returns {"_rid":"","Databases":[...],"_count":N})
  Resource links follow the pattern: dbs/{db}.
  All responses must include the x-ms-request-charge header (set to "1" for emulation).
  milestone: v1.7-beta
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: Data plane — Collection (Container) operations
  Implement the Cosmos DB REST API collection resource endpoints:
  - POST   /{dbs}/{db}/colls                   – create collection (body includes id, partitionKey, indexingPolicy, defaultTtl; optional x-ms-offer-throughput header)
  - GET    /{dbs}/{db}/colls/{coll}             – get collection
  - DELETE /{dbs}/{db}/colls/{coll}             – delete collection
  - PUT    /{dbs}/{db}/colls/{coll}             – replace collection (update indexingPolicy, defaultTtl)
  - GET    /{dbs}/{db}/colls                    – list collections
  Resource links follow the pattern: dbs/{db}/colls/{coll}.
  Persist collection definitions as JSON files on disk via the resource provider;
  they mirror the ARM-side container created through the control plane.
  milestone: v1.7-beta
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: Data plane — Document (Item) CRUD operations
  Implement the Cosmos DB REST API document resource endpoints:
  - POST   /{dbs}/{db}/colls/{coll}/docs                      – create document (assigns _rid, _self, _etag, _ts; validates partition key presence)
  - GET    /{dbs}/{db}/colls/{coll}/docs/{docId}              – get document (requires x-ms-documentdb-partitionkey header matching stored partition key value)
  - PUT    /{dbs}/{db}/colls/{coll}/docs/{docId}              – replace document (full replace; update _etag, _ts; respect If-Match for optimistic concurrency)
  - PATCH  /{dbs}/{db}/colls/{coll}/docs/{docId}              – partial update via JSON Patch operations array (add, set, replace, remove, increment)
  - DELETE /{dbs}/{db}/colls/{coll}/docs/{docId}              – delete document (requires x-ms-documentdb-partitionkey header)
  - GET    /{dbs}/{db}/colls/{coll}/docs                      – list documents in a collection (returns {"_rid":"...","Documents":[...],"_count":N})
  Documents are stored as individual JSON files under .topaz/cosmos-db/.../colls/{coll}/docs/.
  Filename: {docId}.json (URL-encoded if the id contains special characters).
  Implement If-Match / If-None-Match ETag concurrency checks; return 412 on mismatch.
  milestone: v1.7-beta
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: Data plane — SQL query execution
  Implement the document query endpoint:
  - POST /{dbs}/{db}/colls/{coll}/docs  with header x-ms-documentdb-isquery: true
    and Content-Type: application/query+json
    Body: {"query": "SELECT * FROM c WHERE c.field = @val", "parameters": [{"name": "@val", "value": 42}]}
  Implement a minimal SQL subset sufficient for the .NET SDK and Azure CLI:
  - SELECT with scalar and wildcard projections (SELECT *, SELECT c.field, SELECT VALUE c.field)
  - FROM with a single collection alias (FROM c)
  - WHERE with equality (=), inequality (<, >, <=, >=, !=), IN, BETWEEN, IS_NULL, IS_DEFINED, IS_STRING, IS_NUMBER, IS_BOOL operators on top-level and nested properties
  - ORDER BY on a single property (ASC/DESC)
  - OFFSET/LIMIT
  - COUNT, SUM, MIN, MAX, AVG aggregate functions
  - Parameterised queries (@name substitution)
  Pagination: honour x-ms-max-item-count; return x-ms-continuation token when more
  results exist; accept x-ms-continuation on follow-up requests.
  Return: {"_rid":"...","Documents":[...],"_count":N} with x-ms-request-charge header.
  milestone: v1.7-beta
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: MCP Server provisioning tools for Cosmos DB
  Extend Topaz.MCP with Cosmos DB provisioning tools:
  - CreateCosmosDbAccount — create a DatabaseAccount with SQL API in a resource group
  - CreateCosmosDbDatabase — create a SQL database under an existing account
  - CreateCosmosDbContainer — create a SQL container with a specified partitionKey path
  Extend GetConnectionStrings to include the Cosmos DB AccountEndpoint and AccountKey
  for provisioned accounts.
  milestone: v1.7-beta
  labels: enhancement, cosmos-db, mcp
-->

<!--
TODO: VirtualNetwork — Public IP Address (PIP) CRUD
  Implement PUT/GET/DELETE/LIST/PATCH endpoints for Microsoft.Network/publicIPAddresses.
  Required to support `az vm create` which allocates a public IP before creating the NIC.
  Properties: publicIPAllocationMethod (Dynamic/Static), publicIPAddressVersion (IPv4/IPv6),
  ipAddress (stub value assigned on creation), provisioningState ("Succeeded").
  Register Deploy() in TemplateDeploymentOrchestrator for "Microsoft.Network/publicIPAddresses".
  milestone: v1.7-beta
  labels: enhancement, good first issue
-->

<!--
TODO: Topaz CLI — configurable defaults
  Add a `topaz configure` command (and `topaz configure list` sub-command) that persists
  per-user default values for the most commonly repeated flags:
  - --subscription-id  (default subscription GUID)
  - --resource-group   (default resource group name)
  - --location         (default Azure region)
  Defaults are stored in a JSON config file inside the Topaz data directory
  (e.g. ~/.topaz/defaults.json or alongside global-dns.json).
  All existing commands that accept these flags should read from the config file when
  the flag is not explicitly supplied on the command line, following the same precedence
  as the Azure CLI: explicit flag > environment variable > config file default.
  Include CLI tests covering: set a default, verify it is applied by a downstream command,
  and clear/override a default.
  milestone: v1.7-beta
  labels: enhancement, cli, good first issue
-->

### Azure App Service — Kudu / SCM data plane

<!--
TODO: Azure App Service: Kudu SCM data-plane endpoints (zip deploy + deployment list)
  Implement a minimal Kudu/SCM data-plane for Microsoft.Web/sites on a dedicated
  port (GlobalSettings.DefaultAppServiceKuduPort, 8896).
  Site identity is resolved from the Host header:
    {siteName}.scm.azurewebsites.topaz.local.dev
  New model: DeploymentRecord with Id (GUID string), Status ("succeeded"/"pending"/"failed"),
  StartTime, EndTime (DateTimeOffset), Message (optional), Deployer ("topaz").
  Endpoints (one file each under Services/Topaz.Service.AppService/Endpoints/Kudu/):
  - POST /api/zipdeploy
    Read the request body (zip archive), store it at
    .topaz/{sub}/{rg}/.azure-web-sites/{name}/deployments/{id}.zip,
    persist a DeploymentRecord at
    .topaz/{sub}/{rg}/.azure-web-sites/{name}/deployments/{id}/metadata.json,
    and return 202 Accepted with a Location: /api/deployments/{id} header.
  - GET /api/deployments
    List all DeploymentRecord metadata files for the resolved site and return them
    as a JSON array.
  Add a new AppServiceKuduService (IServiceDefinition) referencing both endpoints on
  DefaultAppServiceKuduPort / Protocol.Https, and register it in Topaz.Host/Host.cs.
  Prerequisite: port constant DefaultAppServiceKuduPort = 8896 and DNS suffix constant
  AppServiceKuduDnsSuffix = "scm.azurewebsites.topaz.local.dev" must be added to
  GlobalSettings as part of the v1.5-beta ARM control-plane work.
  milestone: v1.7-beta
  labels: enhancement, app-service
-->

---

## v1.8-preview

### Azure Storage — Blob authentication enforcement

<!--
TODO: Azure Blob Storage: enforce authentication on private containers
  Unauthenticated requests (no Authorization header and no SAS query string) to Blob
  endpoints where the container's public-access level is `none` (the default) currently
  succeed. Real Azure returns 401 with a WWW-Authenticate challenge in this case.
  Required changes:
  - In the Blob data-plane security provider, after SAS and Bearer-token checks,
    check whether the container's stored public-access level is `none`.
  - If the request is unauthenticated and the container is private, return 401 with
    header: WWW-Authenticate: Bearer authorization_uri="https://login.microsoftonline.com"
  - This applies to all data-plane GET/HEAD/PUT/DELETE/POST Blob endpoint paths that
    operate on objects within a named container.
  - Shared container operations (e.g. `?comp=list` on the account root) are not affected.
  milestone: v1.8-preview
  labels: enhancement, storage, good first issue
-->

### Azure App Service — transparent request forwarding

<!--
TODO: Azure App Service: transparent HTTP request forwarding (data-plane hosting)
  Implement a new data-plane forwarding endpoint on DefaultAppServicePort (8895) that
  proxies all HTTP traffic to the user's actual application container, enabling Docker
  Compose setups where Topaz emulates the App Service hosting layer.
  Behaviour:
  1. Extract the site name from the incoming Host header:
       {siteName}.azurewebsites.topaz.local.dev:{8895}
  2. Load the AppServiceSiteResource via AppServiceSiteResourceProvider and read the
     WEBSITES_PORT app setting from SiteConfig.AppSettings (default: 80 if not set).
     This mirrors real Azure App Service behaviour for custom-port containers.
  3. Build the forwarding target URL:
       http://{siteName}:{WEBSITES_PORT}{path}{query}
     where {siteName} is assumed to be the Docker Compose service name (convention:
     the Compose service must be named identically to the App Service site).
  4. Forward the request (method, headers, body) via HttpClient and stream the response
     (status code, headers, body) back to the caller verbatim.
  5. Propagate X-Forwarded-For and X-Forwarded-Host headers.
  Design notes:
  - Register a named HttpClient / IHttpClientFactory in Topaz.Host for connection pooling.
  - Add the forwarding endpoint to AppServiceSiteService.Endpoints on DefaultAppServicePort.
  - IEndpointDefinition.GetResponse is currently synchronous; evaluate whether an async
    overload should be introduced before beginning this work, or accept .GetAwaiter().GetResult()
    as a temporary approach for the dev-emulator context.
  Compose integration:
  - Add port 8895 to Examples/Compose/docker-compose.yml.
  - Add an extra_hosts entry mapping {siteName}.azurewebsites.topaz.local.dev to Topaz's IP.
  - Document WEBSITES_PORT usage in the example.
  milestone: v1.8-preview
  labels: enhancement, app-service
-->

---

## Unplanned / Ideas

_Rough ideas not yet tied to a specific version._

<!--
TODO: OpenTofu integration — verified compatibility and dedicated test suite
  OpenTofu is the Linux Foundation fork of Terraform (BSL → MPL 2.0) with growing
  enterprise adoption. The azurerm provider is 100% compatible with existing Topaz
  Terraform infrastructure, so the implementation cost is low:
  - Add a Topaz.Tests.OpenTofu project mirroring Topaz.Tests.Terraform (swap the
    Terraform binary for the OpenTofu binary in the Testcontainers fixture).
  - Add a build-opentofu-container.sh script.
  - Add an OpenTofu integration guide to website/docs/integrations/.
  - Flip the relevant cells in website/docs/api-coverage/ to note OpenTofu support.
  labels: enhancement, good first issue
-->
