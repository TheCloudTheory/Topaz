# Backlog

Central place for planning upcoming work. Each TODO block below is automatically
converted to a GitHub Issue by CI when new lines are committed.

> **Note:** The action only picks up TODOs that appear in the *diff* of a new commit.
> To bulk-import existing items, run the _TODO to Issue_ workflow manually from the
> **Actions** tab.

---

## Format reference

```markdown
<!--
TODO: Short issue title (required)
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

## Unplanned / Ideas

_Rough ideas not yet tied to a specific version._
