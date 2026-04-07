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

### Queue Storage — preview

<!--
TODO: Queue Storage: Service-level operations
  Implement Queue Storage service operations on the storage data-plane port:
  - GET  /?comp=list         – list queues
  - GET  /?comp=properties   – get service properties
  - PUT  /?comp=properties   – set service properties
  - GET  /?comp=stats        – get service stats
  Mirror the approach used for Blob and Table storage under Topaz.Service.Storage.
  milestone: v1.2-beta
  labels: enhancement, storage
-->

<!--
TODO: Queue Storage: Queue CRUD operations
  Implement per-queue control operations:
  - PUT    /{queue-name}               – create queue
  - DELETE /{queue-name}               – delete queue
  - GET    /{queue-name}?comp=metadata – get queue metadata
  - PUT    /{queue-name}?comp=metadata – set queue metadata
  - GET    /{queue-name}?comp=acl      – get queue ACL
  - PUT    /{queue-name}?comp=acl      – set queue ACL
  milestone: v1.2-beta
  labels: enhancement, storage
-->

<!--
TODO: Queue Storage: Message operations
  Implement the message-level endpoints for Queue Storage:
  - POST   /{queue-name}/messages                – enqueue message
  - GET    /{queue-name}/messages                – dequeue message(s)
  - GET    /{queue-name}/messages?peekonly=true  – peek message(s)
  - PUT    /{queue-name}/messages/{message-id}   – update message visibility / content
  - DELETE /{queue-name}/messages/{message-id}   – delete message
  - DELETE /{queue-name}/messages                – clear all messages
  milestone: v1.2-beta
  labels: enhancement, storage
-->

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
TODO: ARM Deployments: What-If operation
  Implement `POST .../deployments/{name}/whatIf` and `.../deployments/whatIf` (subscription scope).
  Should return a diff of resources that would be created, modified, or deleted.
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

---

## v1.3-beta

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

## Unplanned / Ideas

_Rough ideas not yet tied to a specific version._
