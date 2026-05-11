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
  milestone: v1.4-beta
  labels: enhancement
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
TODO: Key Vault Keys: Key rotation and policy operations
  Implement:
  - POST {vaultBaseUrl}/keys/{name}/rotate           – rotate key
  - GET  {vaultBaseUrl}/keys/{name}/rotationpolicy   – get rotation policy
  - PUT  {vaultBaseUrl}/keys/{name}/rotationpolicy   – update rotation policy
  milestone: v1.2-beta
  labels: enhancement, key-vault
-->

### Azure PowerShell integration

<!-- All three Azure PowerShell items completed in v1.2-beta:
     - install/configure-azure-powershell-cert.ps1
     - install/configure-azure-powershell-env.ps1
     - Topaz.Tests.AzurePowerShell (TopazPowerShellFixture + resource-group + Key Vault tests)
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

<!-- List at subscription scope, management-group scope, and tenant scope all implemented. -->

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

Implemented: `GET /subscriptions/{sub}/providers`, `POST .../register`, `POST .../unregister`. Registration state is persisted per subscription and enforced cross-cutting in the Router (returns 409 `MissingSubscriptionRegistration` for unregistered namespaces).

### Entra ID authentication for Azure Storage

_Implemented: Bearer token (Entra ID) authentication added to Blob, Queue, and Table Storage data-plane endpoints alongside the existing SharedKey/SharedKeyLite mechanism. Full RBAC check via `AzureAuthorizationAdapter.PrincipalHasPermissions`. Returns `401 + WWW-Authenticate` challenge when no Authorization header is present. SharedKey HMAC validation for Blob/Queue now uses the 13-field Blob/Queue StringToSign format. `IEndpointDefinition.Authorize` override in storage base classes bypasses the Router's ARM RBAC check; auth is managed per-request in `IsRequestAuthorized`._

### Azure Virtual Machines — initial control plane

_Implemented in v1.3-beta: service scaffold, five control-plane endpoints (create/update, get, delete, list by resource group, list by subscription), E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests._

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

### Azure Storage — SAS (Shared Access Signature) validation

<!--
TODO: Storage: Account SAS query-string validation for Blob, Queue, and Table
  The ARM `ListAccountSas` and `ListServiceSas` endpoints already generate and return SAS
  token strings. However the data-plane security providers (BlobStorageSecurityProvider,
  QueueStorageSecurityProvider, TableStorageSecurityProvider) only recognise the
  `Authorization:` request header — they ignore query-string SAS parameters entirely.
  A request carrying `?sv=…&sig=…` but no Authorization header hits the missing-header
  fast-path and returns 401.

  Implement Account SAS validation in all three security providers:
  - Detect SAS parameters (sv, ss, srt, sp, se, st, spr, sip, sig) in the query string.
  - Build the StringToSign from the canonical Account SAS format:
    `accountName + permissions + services + resourceTypes + start + expiry + ip + protocol + version`
    (all newline-separated), then HMAC-SHA256 with the account key.
  - Validate that the computed signature matches `sig=`, the expiry (`se=`) has not passed,
    the requested service letter appears in `ss=` (b/q/t/f), the resource type letter
    appears in `srt=` (s/c/o), and the HTTP method is covered by `sp=`.
  - A valid SAS may be used in place of an Authorization header; the request is then
    treated as authorized for the subset of operations the SAS permits.
  milestone: v1.4-beta
  labels: enhancement, storage, security
-->

<!--
TODO: Storage: Service SAS query-string validation for Blob, Queue, and Table
  Service SAS tokens are scoped to a single service and resource (container, blob, queue,
  or table). Implement Service SAS validation:
  - Detect Service SAS parameters (sv, sr, sp, se, st, spr, sip, si, sig, and the
    response-header overrides rscc/rscd/rsce/rscl/rsct) in the query string.
  - Build the StringToSign per service:
    Blob:  `permissions + start + expiry + canonicalizedResource + signedIdentifier +
            signedIP + signedProtocol + signedVersion + signedDirectoryDepth +
            signedEncryptionScope + rscc + rscd + rsce + rscl + rsct`
    Queue: `permissions + start + expiry + canonicalizedResource + signedIdentifier +
            signedIP + signedProtocol + signedVersion`
    Table: `permissions + start + expiry + canonicalizedResource + signedIdentifier +
            signedIP + signedProtocol + signedVersion + startPk + startRk + endPk + endRk`
  - When `si=<policyId>` is present, look up the named policy from the stored
    `<SignedIdentifiers>` XML for the resource (container ACL / queue ACL / table ACL),
    merge the policy's permissions/start/expiry into the validation, and skip those
    fields from the wire token.
  - Validate signature, expiry, IP restrictions, and HTTP method as for Account SAS.
  milestone: v1.4-beta
  labels: enhancement, storage, security
-->

<!--
TODO: Storage: Stored Access Policy enforcement at request time
  The GET/SET Container ACL, Queue ACL, and Table ACL endpoints already persist
  `<SignedIdentifiers>` XML to disk. This data is currently only round-tripped —
  it is never consulted when a Service SAS arrives with `si=<policyId>`.
  As part of the Service SAS implementation, add a lookup path in each security
  provider that:
  - Reads the stored policy XML for the targeted resource.
  - Finds the `<SignedIdentifier>` whose `<Id>` matches `si=`.
  - Merges the policy's `<Start>`, `<Expiry>`, and `<Permission>` values into the
    SAS validation, overriding the corresponding (absent) query parameters.
  - Returns 403 `AuthorizationFailure` if the policy no longer exists (revocation).
  milestone: v1.4-beta
  labels: enhancement, storage, security
-->

<!--
TODO: Storage: Anonymous / public-access reads for Blob containers
  Real Azure allows Blob containers to permit unauthenticated reads when created
  with `x-ms-blob-public-access: container` (list + read) or `blob` (read only).
  Currently any request that reaches a Blob endpoint without an Authorization header
  or SAS token receives 401 unconditionally.
  Implement anonymous read support:
  - Store the public-access level when a container is created or updated
    (`x-ms-blob-public-access` header on PUT /{containerName}?restype=container).
  - In BlobStorageSecurityProvider.RequestIsAuthorized, if no Authorization header
    and no SAS query parameters are present, look up the container's public-access
    level. Allow the request if the access level permits the operation
    (container-level: list blobs + get blob; blob-level: get blob only).
  - Return the public-access level in the `x-ms-blob-public-access` response header
    on GetContainerProperties and GetContainerAcl.
  milestone: v1.4-beta
  labels: enhancement, storage, security
-->

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

### Key Vault — AES symmetric key (oct) cryptographic operations

<!--
TODO: Key Vault Keys: AES symmetric (oct) encrypt/decrypt/wrapKey/unwrapKey support
  The current encrypt/decrypt/wrapKey/unwrapKey implementation only supports RSA keys.
  AES symmetric algorithms (A128GCM, A192GCM, A256GCM, A128CBC, A192CBC, A256CBC,
  A128CBCPAD, A192CBCPAD, A256CBCPAD) require `oct` key type support.
  To implement this:
  - Extend KeyBundle / JsonWebKey to store and expose the `k` field (base64url-encoded
    raw symmetric key material) for oct-type keys.
  - Implement AES-GCM and AES-CBC(PAD) encrypt/decrypt in KeyVaultDataPlane.
  - Wire up the EncryptKeyEndpoint and DecryptKeyEndpoint to dispatch to the AES path
    when the key type is `oct` or `oct-HSM`.
  - Add tests: E2E via CryptographyClient, Azure CLI via `az keyvault key encrypt --algorithm A256GCM`.
  milestone: v1.4-beta
  labels: enhancement, key-vault
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

<!--
TODO: Virtual Networks: Delete, List, List All, and Update Tags operations
  Complete the VNet control plane by adding the missing endpoints:
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks/{name}
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks
  - GET    /subscriptions/{sub}/providers/Microsoft.Network/virtualNetworks
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks/{name} (update tags)
  milestone: v1.4-beta
  labels: enhancement, virtual-network
-->

<!--
TODO: Virtual Networks: Check IP Address Availability
  Implement `GET .../virtualNetworks/{name}/CheckIPAddressAvailability?ipAddress={ip}`.
  Should validate whether the given IP falls within any subnet and is not already allocated.
  milestone: v1.4-beta
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
  milestone: v1.4-beta
  labels: enhancement, virtual-network
-->

### Azure Storage — OData query support for Table Storage

<!--
TODO: Storage: Improved OData $filter / $select / $top handling for Table Storage
  Table Storage exposes a rich OData query interface that Topaz currently lacks.
  The Azure SDK, Azure CLI, and Terraform all emit OData query parameters when
  listing or querying table entities; without proper parsing these parameters are
  silently ignored and callers receive unfiltered full-table results.

  Implement OData query support in the Table Storage data plane:
  - $filter — parse and evaluate simple OData filter expressions against entity
    properties (logical operators: and, or, not; comparison operators: eq, ne, gt,
    ge, lt, le; supported literal types: string, int32, int64, bool, datetime, guid).
    Prioritise the predicates emitted by the Azure SDK and Terraform provider.
  - $select — return only the requested property names in each entity object.
  - $top — limit the number of entities returned per response page.
  - $skiptoken — honour continuation tokens for server-side paging so multi-page
    reads driven by the SDK work correctly.
  Consider using Microsoft.OData.Core (ODataUriParser) for robust OData expression
  parsing rather than ad-hoc string splitting.
  milestone: v1.4-beta
  labels: enhancement, storage
-->

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
  labels: enhancement, resource-manager
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
  labels: enhancement, cosmos-db
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
  labels: enhancement, cosmos-db
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
  labels: enhancement, cosmos-db
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
  labels: enhancement, cosmos-db
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
  labels: enhancement, cosmos-db
-->

---

## v1.7-beta

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

---

## Unplanned / Ideas

_Rough ideas not yet tied to a specific version._
