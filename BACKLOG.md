# Backlog

Central place for planning upcoming work. Each TODO block below is automatically
converted to a GitHub Issue by CI when new lines are committed.

> **Note:** The action only picks up TODOs that appear in the *diff* of a new commit.
> To bulk-import existing items, run the _TODO to Issue_ workflow manually from the
> **Actions** tab.

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

<!--
TODO: Key Vault Secrets: Get Versions endpoint
  Implement `GET {vaultBaseUrl}/secrets/{secret-name}/versions` returning a paged list of
  all versions for the given secret. Follows the same pattern as `GetSecretsEndpoint`.
  milestone: v1.1-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Secrets: Backup and Restore endpoints
  Implement `POST {vaultBaseUrl}/secrets/{secret-name}/backup` (returns an opaque blob)
  and `POST {vaultBaseUrl}/secrets/restore` (accepts the blob and recreates the secret).
  milestone: v1.1-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Secrets: Deleted secrets operations
  Implement the soft-delete data-plane surface:
  - GET  {vaultBaseUrl}/deletedsecrets              – list deleted secrets
  - GET  {vaultBaseUrl}/deletedsecrets/{name}       – get deleted secret
  - POST {vaultBaseUrl}/deletedsecrets/{name}/recover – recover deleted secret
  - POST {vaultBaseUrl}/deletedsecrets/{name}/purge  – purge deleted secret
  milestone: v1.1-beta
  labels: enhancement, key-vault
-->

### Container Registry — data plane preview

<!--
TODO: Container Registry: ACR OAuth2 token endpoint
  Implement `POST /oauth2/token` (refresh-token → access-token exchange).
  The challenge endpoint and refresh-token exchange are already in place; this
  completes the three-step ACR auth flow.
  milestone: v1.1-beta
  labels: enhancement, container-registry
-->

<!--
TODO: Container Registry: Repository and Tag data plane operations
  Implement OCI distribution-spec endpoints for repositories and tags:
  - GET /v2/_catalog                          – list repositories
  - GET /v2/{name}/tags/list                  – list tags
  - DELETE /v2/{name}/manifests/{reference}   – used to delete a tag
  milestone: v1.1-beta
  labels: enhancement, container-registry
-->

<!--
TODO: Container Registry: Manifest data plane operations
  Implement OCI manifest endpoints:
  - GET    /v2/{name}/manifests/{reference}   – get manifest
  - PUT    /v2/{name}/manifests/{reference}   – put manifest
  - DELETE /v2/{name}/manifests/{reference}   – delete manifest
  - HEAD   /v2/{name}/manifests/{reference}   – check existence
  Manifests must be stored per-registry on disk via the resource provider.
  milestone: v1.1-beta
  labels: enhancement, container-registry
-->

<!--
TODO: Container Registry: Blob data plane operations
  Implement OCI blob upload/download endpoints:
  - GET    /v2/{name}/blobs/{digest}           – download blob
  - HEAD   /v2/{name}/blobs/{digest}           – check existence
  - DELETE /v2/{name}/blobs/{digest}           – delete blob
  - POST   /v2/{name}/blobs/uploads/           – start upload session
  - PATCH  /v2/{name}/blobs/uploads/{uuid}     – stream chunk
  - PUT    /v2/{name}/blobs/uploads/{uuid}     – complete upload
  milestone: v1.1-beta
  labels: enhancement, container-registry
-->

---

## v1.2-beta

### Key Vault — keys support

<!--
TODO: Key Vault Keys: Core CRUD operations
  Implement the data-plane key management surface:
  - POST  {vaultBaseUrl}/keys/{name}/create       – create key
  - PUT   {vaultBaseUrl}/keys/{name}              – import key
  - GET   {vaultBaseUrl}/keys/{name}/{version}    – get key
  - PATCH {vaultBaseUrl}/keys/{name}/{version}    – update key attributes
  - DELETE {vaultBaseUrl}/keys/{name}             – delete key
  - GET   {vaultBaseUrl}/keys                     – list keys
  - GET   {vaultBaseUrl}/keys/{name}/versions     – list key versions
  milestone: v1.2-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Keys: Backup and Restore
  Implement:
  - POST {vaultBaseUrl}/keys/{name}/backup    – export opaque key backup blob
  - POST {vaultBaseUrl}/keys/restore          – restore from backup blob
  Follows the same pattern as the planned secret backup/restore endpoints.
  milestone: v1.2-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Keys: Cryptographic operations
  Implement the data-plane crypto surface for keys:
  - POST {vaultBaseUrl}/keys/{name}/{version}/encrypt
  - POST {vaultBaseUrl}/keys/{name}/{version}/decrypt
  - POST {vaultBaseUrl}/keys/{name}/{version}/sign
  - POST {vaultBaseUrl}/keys/{name}/{version}/verify
  - POST {vaultBaseUrl}/keys/{name}/{version}/wrapkey
  - POST {vaultBaseUrl}/keys/{name}/{version}/unwrapkey
  milestone: v1.2-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Keys: Key rotation and policy operations
  Implement:
  - POST {vaultBaseUrl}/keys/{name}/rotate           – rotate key
  - GET  {vaultBaseUrl}/keys/{name}/rotationpolicy   – get rotation policy
  - PUT  {vaultBaseUrl}/keys/{name}/rotationpolicy   – update rotation policy
  milestone: v1.2-beta
  labels: enhancement, key-vault
-->

### Azure PowerShell integration

<!--
TODO: Azure PowerShell: Certificate trust configuration script
  Add `install/configure-azure-powershell-cert.ps1` that appends the Topaz certificate
  to the PowerShell Az module's certificate store, mirroring what
  `install/configure-azure-cli-cert.sh` does for Azure CLI.
  milestone: v1.2-beta
  labels: enhancement, azure-powershell
-->

<!--
TODO: Azure PowerShell: Connect-AzAccount cloud environment registration
  Implement an Az-compatible cloud environment JSON (same structure as the Azure CLI
  cloud environment registered in TopazFixture) so that users can run:
    Add-AzEnvironment -Name "Topaz" -ResourceManagerUrl "https://topaz.local.dev:8899" ...
    Connect-AzAccount -Environment "Topaz" -ServicePrincipal ...
  Provide a ready-made `install/configure-azure-powershell-env.ps1` script that
  registers the environment and authenticates against Topaz.
  milestone: v1.2-beta
  labels: enhancement, azure-powershell
-->

<!--
TODO: Azure PowerShell: Testcontainers-based test fixture and initial test suite
  Add a new test project `Topaz.Tests.AzurePowerShell` following the same
  Testcontainers pattern as `Topaz.Tests.AzureCLI`:
  - `TopazPowerShellFixture` spins up the Topaz container and an mcr.microsoft.com/powershell
    container on a shared Docker network.
  - `RunAzurePowerShellCommand(string script)` helper executes a PowerShell script inside
    the container and returns stdout.
  - Include an initial smoke-test suite covering resource-group and Key Vault operations
    (matching the CLI tests already present) to verify the Az module can authenticate
    and call Topaz successfully.
  milestone: v1.2-beta
  labels: enhancement, azure-powershell, tests
-->

### Management Groups — basic CRUD

<!--
TODO: Management Groups: Core CRUD operations
  Implement the management group resource surface:
  - PUT    /providers/Microsoft.Management/managementGroups/{groupId}  – create or update
  - GET    /providers/Microsoft.Management/managementGroups/{groupId}  – get
  - DELETE /providers/Microsoft.Management/managementGroups/{groupId}  – delete
  - PATCH  /providers/Microsoft.Management/managementGroups/{groupId}  – update (rename / change parent)
  - GET    /providers/Microsoft.Management/managementGroups             – list
  milestone: v1.2-beta
  labels: enhancement, management-groups
-->

### ARM Deployments — full support

<!--
TODO: ARM Deployments: Cancel operation
  Implement `POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Resources/deployments/{name}/cancel`.
  Should mark an in-progress deployment as cancelled.
  milestone: v1.2-beta
  labels: enhancement, resource-manager
-->

<!--
TODO: ARM Deployments: Export Template endpoint
  Implement `POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Resources/deployments/{name}/exportTemplate`.
  Returns the ARM template that was used for the deployment.
  milestone: v1.2-beta
  labels: enhancement, resource-manager
-->

<!--
TODO: ARM Deployments: List at subscription, management-group, and tenant scope
  Implement the remaining list-deployments endpoints:
  - GET /subscriptions/{sub}/providers/Microsoft.Resources/deployments
  - GET /providers/Microsoft.Management/managementGroups/{mgId}/providers/Microsoft.Resources/deployments
  - GET /providers/Microsoft.Resources/deployments
  milestone: v1.2-beta
  labels: enhancement, resource-manager
-->

### Packaging — CLI and Host split

<!--
TODO: Split Topaz into separate CLI and Host executables
  Separate the current monolithic Topaz CLI into two distinct artifacts:
  - `topaz-host` — the standalone Host process that runs all emulated Azure services
  - `topaz-cli` — a thin CLI tool that communicates with a running Host instance
  Each artifact should be published as an independent binary and Docker image.
  This is a breaking change: existing `topaz` CLI invocations and Docker image
  references will need to be updated.
  milestone: v1.2-beta
  labels: enhancement, breaking-change
-->

---

## v1.3-beta

### Management Groups — extended operations

<!--
TODO: Management Groups: Get Descendants
  Implement `GET /providers/Microsoft.Management/managementGroups/{groupId}/descendants`.
  Returns all child management groups and subscriptions under a given management group.
  milestone: v1.3-beta
  labels: enhancement, management-groups
-->

<!--
TODO: Management Groups: Management Group Subscriptions
  Implement subscription association endpoints:
  - PUT    /providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}  – associate subscription
  - DELETE /providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}  – disassociate subscription
  - GET    /providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}  – get subscription under MG
  milestone: v1.3-beta
  labels: enhancement, management-groups
-->

<!--
TODO: Management Groups: Hierarchy Settings
  Implement tenant-level hierarchy settings endpoints:
  - PUT    /providers/Microsoft.Management/managementGroups/{groupId}/settings/default  – create or update
  - DELETE /providers/Microsoft.Management/managementGroups/{groupId}/settings/default  – delete
  - GET    /providers/Microsoft.Management/managementGroups/{groupId}/settings/default  – get
  - GET    /providers/Microsoft.Management/managementGroups/{groupId}/settings           – list
  - PATCH  /providers/Microsoft.Management/managementGroups/{groupId}/settings/default  – update
  milestone: v1.3-beta
  labels: enhancement, management-groups
-->

<!--
TODO: Management Groups: Entities list
  Implement `GET /providers/Microsoft.Management/getEntities`.
  Returns all management groups and subscriptions accessible by the caller.
  milestone: v1.3-beta
  labels: enhancement, management-groups
-->

### Resource Providers — operations support

<!--
TODO: Resource Providers: List, Register, and Unregister operations
  Extend the existing `GET /providers/{namespace}` (already implemented) with:
  - GET  /subscriptions/{sub}/providers           – list all providers
  - POST /subscriptions/{sub}/providers/{namespace}/register   – register provider
  - POST /subscriptions/{sub}/providers/{namespace}/unregister – unregister provider
  milestone: v1.3-beta
  labels: enhancement, resource-manager
-->

### Virtual Networks — full control plane

<!--
TODO: Virtual Networks: Delete, List, List All, and Update Tags operations
  Complete the VNet control plane by adding the missing endpoints:
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks/{name}
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks
  - GET    /subscriptions/{sub}/providers/Microsoft.Network/virtualNetworks
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks/{name} (update tags)
  milestone: v1.3-beta
  labels: enhancement, virtual-network
-->

<!--
TODO: Virtual Networks: Check IP Address Availability
  Implement `GET .../virtualNetworks/{name}/CheckIPAddressAvailability?ipAddress={ip}`.
  Should validate whether the given IP falls within any subnet and is not already allocated.
  milestone: v1.3-beta
  labels: enhancement, virtual-network
-->

<!--
TODO: Subnets: Full CRUD operations
  Implement the subnet resource surface under Virtual Networks:
  - PUT    .../virtualNetworks/{vnet}/subnets/{name}  – create or update
  - GET    .../virtualNetworks/{vnet}/subnets/{name}  – get
  - DELETE .../virtualNetworks/{vnet}/subnets/{name}  – delete
  - GET    .../virtualNetworks/{vnet}/subnets          – list
  Follow the resource provider pattern used by other Topaz.Service.VirtualNetwork resources.
  milestone: v1.3-beta
  labels: enhancement, virtual-network
-->

<!--
TODO: Network Security Groups: Full control plane
  Add a new NSG resource under Topaz.Service.VirtualNetwork (new models, resource provider, and endpoints):
  - PUT    .../networkSecurityGroups/{name}  – create or update
  - GET    .../networkSecurityGroups/{name}  – get
  - DELETE .../networkSecurityGroups/{name}  – delete
  - GET    .../networkSecurityGroups          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Network/networkSecurityGroups – list all
  - PATCH  .../networkSecurityGroups/{name}  – update tags
  milestone: v1.3-beta
  labels: enhancement, virtual-network
-->

### Entra ID authentication for Azure Storage

<!--
TODO: Azure Storage: Entra ID bearer-token authentication for Blob and Table data plane
  Allow the Blob Storage and Table Storage data-plane endpoints to accept requests
  authenticated with an Entra ID bearer token (Authorization: Bearer <token>) in addition
  to the existing shared-key mechanism.
  Implementation steps:
  - Validate incoming Bearer tokens against the Topaz Entra ID service (reuse the token
    validation logic already used by other services).
  - Map the token's `oid` claim to a storage account via the existing ResourceProvider.
  - Return 401 with a `WWW-Authenticate: Bearer ...` challenge when no valid token or
    shared key is present, matching the real Azure Storage OAuth error contract.
  - Update BlobStorageService and TableStorageService to opt-in to the auth middleware.
  milestone: v1.3-beta
  labels: enhancement, storage, security
-->

### Azure Virtual Machines — initial control plane

<!--
TODO: Azure Virtual Machines: New service project scaffold
  Create Topaz.Service.VirtualMachine following the existing service conventions:
  - Project file with references to Topaz.ResourceManager and Topaz.Service.Shared
  - VirtualMachineResourceProperties + VirtualMachineResource (ArmResource<T>)
  - VirtualMachineResourceProvider (ResourceProviderBase<T>) for filesystem persistence
  - VirtualMachineServiceControlPlane implementing IControlPlane with Deploy()
  - IServiceDefinition registration and wiring in Topaz.Host
  - ProjectReference in Topaz.Service.ResourceManager for template deployment routing
  milestone: v1.3-beta
  labels: enhancement, virtual-machines
-->

<!--
TODO: Azure Virtual Machines: Core control plane endpoints
  Implement the initial VM control plane surface:
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name} – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name} – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name} – delete
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines         – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Compute/virtualMachines                             – list all
  This is an emulation — VMs do not actually boot. The resource is persisted to disk and
  reported as "Succeeded" with a stable provisioningState.
  milestone: v1.3-beta
  labels: enhancement, virtual-machines
-->

### Key Vault — full certificate operations support

<!--
TODO: Key Vault Certificates: Core CRUD operations
  Implement the data-plane certificate management surface:
  - POST  {vaultBaseUrl}/certificates/{name}/create         – create certificate (or begin async CSR)
  - POST  {vaultBaseUrl}/certificates/{name}/import         – import PEM/PFX certificate
  - GET   {vaultBaseUrl}/certificates/{name}/{version}      – get certificate
  - PATCH {vaultBaseUrl}/certificates/{name}/{version}      – update certificate attributes
  - DELETE {vaultBaseUrl}/certificates/{name}               – delete certificate
  - GET   {vaultBaseUrl}/certificates                       – list certificates
  - GET   {vaultBaseUrl}/certificates/{name}/versions       – list certificate versions
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Certificates: Backup and Restore
  Implement:
  - POST {vaultBaseUrl}/certificates/{name}/backup   – export opaque certificate backup blob
  - POST {vaultBaseUrl}/certificates/restore         – restore from backup blob
  Follows the same pattern as the secret and key backup/restore endpoints.
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Certificates: Certificate contacts
  Implement vault-level certificate administrator contact management:
  - PUT    {vaultBaseUrl}/certificates/contacts  – set contacts
  - GET    {vaultBaseUrl}/certificates/contacts  – get contacts
  - DELETE {vaultBaseUrl}/certificates/contacts  – delete all contacts
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Certificates: Certificate issuers
  Implement certificate issuer (CA) management:
  - PUT    {vaultBaseUrl}/certificates/issuers/{issuerName}  – create or update issuer
  - GET    {vaultBaseUrl}/certificates/issuers/{issuerName}  – get issuer
  - PATCH  {vaultBaseUrl}/certificates/issuers/{issuerName}  – update issuer
  - DELETE {vaultBaseUrl}/certificates/issuers/{issuerName}  – delete issuer
  - GET    {vaultBaseUrl}/certificates/issuers               – list issuers
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Certificates: Pending certificate operations
  Implement in-flight certificate operation endpoints (used when creation is async / CSR-based):
  - GET    {vaultBaseUrl}/certificates/{name}/pending          – get pending operation
  - PATCH  {vaultBaseUrl}/certificates/{name}/pending          – update (e.g. merge CSR response)
  - DELETE {vaultBaseUrl}/certificates/{name}/pending          – cancel pending operation
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Certificates: Merge certificate
  Implement:
  - POST {vaultBaseUrl}/certificates/{name}/pending/merge  – merge an externally-signed certificate
    (from a CA response) with the pending CSR stored in Key Vault.
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault Certificates: Soft-delete surface
  Implement the soft-delete data-plane surface for certificates:
  - GET  {vaultBaseUrl}/deletedcertificates              – list deleted certificates
  - GET  {vaultBaseUrl}/deletedcertificates/{name}       – get deleted certificate
  - POST {vaultBaseUrl}/deletedcertificates/{name}/recover – recover deleted certificate
  - POST {vaultBaseUrl}/deletedcertificates/{name}/purge   – purge deleted certificate
  milestone: v1.3-beta
  labels: enhancement, key-vault
-->

### MCP Server — resource provisioning and tooling

<!--
TODO: MCP: Resource provisioning tools for Key Vault, Service Bus, and Storage
  Extend Topaz.MCP with tools that provision the most common Azure resources into a running Topaz instance:
  - CreateResourceGroup — create a resource group in a subscription
  - CreateKeyVault — create a Key Vault and optionally seed a secret
  - CreateServiceBusNamespace / CreateServiceBusQueue / CreateServiceBusTopic — Service Bus topology
  - CreateStorageAccount / CreateBlobContainer — Storage resources
  Each tool follows the existing SubscriptionTool.cs pattern using TopazArmClient REST calls.
  milestone: v1.3-beta
  labels: enhancement, mcp
-->

<!--
TODO: MCP: Event Hub and Container Registry provisioning tools
  Add MCP tools for the remaining commonly-used services:
  - CreateEventHubNamespace / CreateEventHub
  - CreateContainerRegistry
  milestone: v1.3-beta
  labels: enhancement, mcp
-->

<!--
TODO: MCP: GetConnectionStrings tool
  Add a GetConnectionStrings tool that queries the running Topaz instance and returns
  ready-to-use connection strings and URIs for all provisioned resources in a subscription:
  - Storage account connection strings
  - Service Bus connection strings
  - Key Vault URI
  - Event Hub connection strings
  - Container Registry login server
  This closes the provisioning workflow: after creating resources the developer immediately
  gets the values needed to configure their application or CI environment.
  milestone: v1.3-beta
  labels: enhancement, mcp
-->

<!--
TODO: MCP: GetTopazStatus diagnostics tool
  Add a GetTopazStatus tool that calls the Topaz health-check endpoint and returns
  the running version, which services are up, and which ports are bound.
  Useful for debugging when an MCP-assisted setup fails partway through.
  milestone: v1.3-beta
  labels: enhancement, mcp
-->

<!--
TODO: MCP: Pre-defined prompts for common setup scenarios
  Implement MCP prompts (the Prompts capability of the MCP protocol) for common
  development environment patterns:
  - "Set up a local Azure environment for a .NET microservice" — guides the AI to create
    resource group → storage account → service bus namespace + queue → key vault → output connection strings
  - "Bootstrap a CI environment" — subscription → registry → storage → env var export
  Prompts compose the existing provisioning tools into guided multi-step workflows.
  milestone: v1.3-beta
  labels: enhancement, mcp
-->

---

## v1.4-beta

### Topaz Portal — tag editing

<!--
TODO: Topaz Portal: Edit existing tag values inline
  Add inline editing to the Tags tab for all resources that support tags
  (Subscriptions, Resource Groups, Key Vaults, and any future tag-capable resources).
  The TagsPanel component should gain an edit mode per row — clicking an Edit button
  makes the Value cell an input, with Save / Cancel buttons. Saving calls the existing
  CreateOrUpdateXxxTag method (upsert semantics) and reloads the tag list.
  milestone: v1.4-beta
  labels: enhancement, portal
-->

### Key Vault — automated soft-delete purging

<!--
TODO: Key Vault: Automated purging of soft-deleted vaults
  Implement a background scheduler that runs periodically and permanently removes
  soft-deleted Key Vault instances whose `scheduledPurgeDate` has passed.
  The scheduler should:
  - Scan all soft-deleted vaults across all subscriptions
  - Compare `scheduledPurgeDate` against the current time
  - Invoke the existing purge logic (remove from disk and DNS entries) for expired vaults
  - Log each purge action at the Debug level
  The scheduler interval should be configurable via GlobalSettings (default: 1 hour).
  milestone: v1.4-beta
  labels: enhancement, key-vault
-->

<!--
TODO: Key Vault: Automated purging of soft-deleted secrets
  Implement a background scheduler that runs periodically and permanently removes
  soft-deleted Key Vault secrets whose `scheduledPurgeDate` has passed.
  The scheduler should:
  - Scan all vaults across all subscriptions
  - For each vault, inspect the `deleted/` subfolder for secret records
  - Compare each record's `scheduledPurgeDate` against the current time
  - Delete expired records from disk
  - Log each purge action at the Debug level
  The scheduler interval should be configurable via GlobalSettings (default: 1 hour).
  milestone: v1.4-beta
  labels: enhancement, key-vault
-->

### Storage Account — geo-replication semantics

<!--
TODO: Storage Account: Full geo-replicated (RA-GRS/RA-GZRS) semantics
  Implement full emulation of geo-redundant storage account behaviour for accounts
  created with Standard_RAGRS or Standard_RAGZRS SKUs.
  Scope:
  - Register secondary DNS hostnames ({accountName}-secondary.*) for RA-GRS accounts
    so that SDK clients can resolve and reach the secondary endpoint
  - Return a valid secondary endpoint URLs in the storage account ARM response
    (secondaryEndpoints.blob, .table, .queue, .file)
  - Implement `GET ?restype=service&comp=stats` for Blob, Table, and Queue secondary
    endpoints returning a realistic GeoReplicationStats payload (status: live,
    lastSyncTime: current time)
  - Return 403 FeatureNotSupported for the stats endpoint on non-RA-GRS accounts
    (already done for Table; extend to Blob and Queue service endpoints)
  - Enforce read-only behaviour on secondary endpoints: mutating operations (PUT, DELETE,
    POST) should return 403 WriteOperationNotSupportedOnSecondary
  milestone: v1.4-beta
  labels: enhancement, storage
-->

---

## v1.5-beta

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
  labels: enhancement, container-registry
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
  labels: enhancement, azure-sql
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
  labels: enhancement, azure-sql
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
  labels: enhancement, azure-sql
-->

---

## v1.6-beta

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

---

## Unplanned / Ideas

_Rough ideas not yet tied to a specific version._
