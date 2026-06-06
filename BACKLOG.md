# Backlog

Central place for planning upcoming work. Each TODO block below is automatically
converted to a GitHub Issue by CI when new lines are committed.

> **Note:** The action only picks up TODOs that appear in the *diff* of a new commit.
> To bulk-import existing items, run the _TODO to Issue_ workflow manually from the
> **Actions** tab.

---

<!--
TODO: Storage — Revoke User Delegation Keys
  Implement POST .../storageAccounts/{name}/revokeUserDelegationKeys ARM endpoint.
  Should persist a per-account revocation timestamp; User Delegation SAS validation must reject
  keys whose skt (signed key start) predates the revocation timestamp.
  milestone: v1.8-preview
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

_Implemented in v1.6-beta: standard GET operations (blob container listing, queue listing, table entity reads) on `{accountName}-secondary.*` endpoints for RA-GRS/RA-GZRS accounts now return data from the same in-memory store as the primary endpoint. Note: secondary reads are always perfectly in sync with the primary — there is no replication lag simulation. `<LastSyncTime>` in stats responses still reflects wall-clock time rather than a scheduler tick. Replication lag simulation is tracked in the v1.9-preview backlog item below._

### Virtual Networks — full control plane

_Implemented in v1.4-beta: `GET .../virtualNetworks/{name}/checkIPAddressAvailability?ipAddress={ip}`. Returns `available: true` when the IP falls within any subnet CIDR, `false` otherwise. `availableIPAddresses` is always `[]` (IP allocation tracking planned for v1.5-beta — see known-limitations.md)._

_Implemented: DELETE, List by resource group, List by subscription, and Update Tags operations._

---

## v1.5-beta

### VirtualNetwork — Network Interface (NIC) CRUD

_Implemented in v1.5-beta: six control-plane endpoints (Create/Update, Get, Delete, List by resource group, List by subscription, Update Tags) for `Microsoft.Network/networkInterfaces`. Static and dynamic private IP allocation via `IpAllocationRegistry` with automatic address assignment from subnet CIDR ranges. Full `Deploy()` support for ARM template deployments. Includes E2E SDK tests and Azure CLI tests._

### Azure Storage — User Delegation SAS for Blob

_Implemented in v1.5-beta: `POST /?restype=service&comp=userdelegationkey` data-plane endpoint issues user delegation keys derived deterministically from the account key and the caller's Entra OID/TID via HMAC-SHA256; requires Bearer auth (SharedKey callers receive `403 AuthenticationFailed`). `UserDelegationSasValidator` re-derives the key at validation time and validates incoming User Delegation SAS tokens for blob (`sr=b`) and container (`sr=c`) scopes. Revocation (`revokeUserDelegationKeys`) is tracked separately in v1.8-preview._

### ARM Deployments — full tenant-scope surface

_Implemented in v1.5-beta: `PUT`, `GET`, `DELETE`, `HEAD`, `validate`, `cancel`, `exportTemplate`, and `whatif` at tenant scope. All eight operations follow the subscription-scope pattern without a `subscriptionId` segment. Includes MCP tools (`CreateOrUpdateTenantDeployment`, `GetTenantDeployment`, `DeleteTenantDeployment`), E2E SDK tests, and Azure CLI tests._

### Container Registry — ACR Tasks

_Implemented in v1.5-beta: ARM-level task management surface (Create, Get, Get Details, List, Update, Delete) under the ACR Tasks API (2019-04-01). Tasks are persisted as subresources of the registry. Complex fields (platform, step, trigger, agentConfiguration, credentials) are stored and round-tripped verbatim as JSON._

_Implemented in v1.5-beta: Task Run lifecycle surface — POST .../tasks/{task}/run, GET/PATCH .../runs/{run}, GET .../runs, POST .../runs/{run}/listLogSasUrl, POST .../scheduleRun. Runs report provisioningState: Succeeded immediately with a static log body. Real Docker execution deferred to v1.7-beta._

<!--
TODO: ACR Tasks: Real Docker build-and-push execution
  Upgrade the ACR run emulation from immediate-Succeeded to real container workload execution:
  - Detect Docker availability at host startup; log a warning if absent.
  - For DockerBuildRequest step type: git clone contextPath, run `docker build`, `docker push` to the local OCI registry.
  - Drive real status transitions: Queued → Running → Succeeded / Failed.
  - Stream actual build output from the docker build process to the log content endpoint (GET /v2/runs/{runId}/log).
  - For non-DockerBuildRequest step types (FileTaskRunRequest, EncodedTaskRunRequest, TaskRunRequest): keep immediate-Succeeded behaviour as a no-op.
  milestone: v1.7-beta
  labels: enhancement, container-registry
-->

### Azure SQL — initial control plane

_Implemented in v1.5-beta: server CRUD (Create/Update, Get, Delete, List by resource group, List by subscription, PATCH update) and database CRUD (Create/Update, Get, Delete, List by server, PATCH update) for `Microsoft.Sql/servers` and `Microsoft.Sql/servers/databases`. Also includes read-only companion endpoints: Transparent Data Encryption (Get/CreateOrUpdate), Connection Policy (Get/CreateOrUpdate), Vulnerability Assessment (Get/CreateOrUpdate), Database Security Alert Policy (Get), Database Backup Short-Term and Long-Term Retention Policies (Get), and Restorable Dropped Databases (List)._

### Azure App Service — initial control plane

_Implemented in v1.5-beta: App Service Plan CRUD (Create/Update, Get, Delete, List by resource group, List by subscription, Restart Sites) and Web App/Site CRUD (Create/Update, Get, Delete, List by resource group, List by subscription) for `Microsoft.Web/serverfarms` and `Microsoft.Web/sites`. Additional site-level endpoints: Get/Update Site Config Web, Get/Update App Settings, List App Settings, Get Slot Config Names, Post Publish XML, Check Name Availability, and Get Web App Stacks._

### Event Hub — delete individual Event Hub

_Implemented in v1.5-beta: `DELETE .../namespaces/{namespaceName}/eventhubs/{eventHubName}` returns `200 OK` on success and `404` when the hub does not exist. Follows the one-file-per-operation convention (`DeleteEventHubEndpoint`). Includes E2E SDK test and Azure CLI tests._

### Virtual Machines — PATCH update endpoint

_Implemented in v1.5-beta: `PATCH .../virtualMachines/{vmName}` for partial updates — tags and hardware/storage/network profiles can be updated without supplying a full VM body. `OsProfile` is intentionally excluded (matches real Azure behaviour). The `az vm update` and Azure SDK `begin_update()` flows are fully supported. Includes E2E SDK test and Azure CLI test._

---

## v1.6-beta

### Key Vault — correct challenge resource in `WWW-Authenticate`

_Implemented in v1.6-beta: `KeyVaultAuthorizationChecker.BuildWwwAuthenticateChallenge` now derives `resource` from the incoming request's `Host` header instead of the hard-coded `https://vault.azure.net`. Python and other non-.NET SDK clients that verify the challenge resource against the vault URL now work without any `verify_challenge_resource=False` workaround._

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

_Implemented in v1.6-beta: cooperative cancellation via `CancellationTokenSource` per deployment. `CancelDeployment` now signals the token when the deployment is `Running`; `RouteDeployment` checks it after each resource provision and transitions `provisioningState` to `Canceled`, leaving already-provisioned resources in place. All four ARM scopes (resource group, subscription, tenant, management group) support mid-flight cancel. Existing 409 behaviour for already-completed deployments is preserved._

### Azure Storage — unified data-plane port

_Implemented in v1.6-beta: all storage data-plane sub-services (Blob, Table, Queue, File) now share a single HTTPS port (`DefaultStoragePort = 8891`). The Router gained a `RequiredHostServiceLabel` interface member so blob/table/queue endpoints co-exist on the same port and are disambiguated by the Host header subdomain (e.g. `{account}.blob.*`, `{account}.table.*`). The four old port constants are aliased to `DefaultStoragePort`. `GetMetadataEndpointResponse.Suffixes["storage"]` updated to `:8891`, removing the `ParseAccountID` port-matching workaround. `TopazResourceHelpers.GetAzureStorageConnectionString` updated; fixed `DefaultEndpointsProtocol=http` → `https` and missing trailing `/` on Table endpoint. All E2E, AzureCLI, and Terraform tests updated._

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

Implemented: SQL container control plane endpoints (Create/Update, Get, Delete, List, Get Throughput, Update Throughput) with partition key, indexing policy, unique key policy and throughput support.

<!--
TODO: Azure Cosmos DB: SQL Container — TTL enforcement
  Background scheduler that purges expired documents from SQL containers where defaultTtl is set.
  Prerequisite: data-plane document store (v1.7-beta).
  milestone: v1.9-preview
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: SQL Container — container-level RBAC
  Per-container access policy enforcement integrated with the data-plane auth layer.
  Prerequisite: data-plane authentication surface (v1.7-beta).
  milestone: v1.9-preview
  labels: enhancement, cosmos-db
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

### Entra — AuthorizeEndpoint `response_mode=form_post`

_Implemented in v1.6-beta: `AuthorizeEndpoint` now reads `response_mode` from the query string. When `form_post` is requested, it returns `200 OK` with an HTML auto-submit form that POSTs `code` and `state` to `redirect_uri` instead of issuing a `302` redirect. This fixes compatibility with Azure CLI 2.86.0 (MSAL), which sets `response_mode=form_post` and rejects redirect responses with "response_mode=query is not supported"._

### Entra — Device Code flow (`POST /devicecode` + `grant_type=device_code`)

_Implemented in v1.6-beta: `DeviceCodeEndpoint` handles `POST /organizations|{tenantId}|common/oauth2/v2.0/devicecode` and returns a valid RFC 8628 device-authorization response (`device_code`, `user_code`, `verification_uri`, `expires_in`, `interval`, `message`). `TokenEndpoint` now handles `grant_type=urn:ietf:params:oauth:grant-type:device_code`. When a `login_hint` is present in the device code request the matching user is resolved; otherwise the global admin is used as a placeholder (see TODO below)._

<!--
TODO: Entra — implement /devicelogin for real device code browser sign-in
  DeviceCodeEndpoint currently pre-binds the device code to the global admin when no
  login_hint is provided, so token polling succeeds immediately. Real Azure keeps the
  code in "authorization_pending" state until the user visits verification_uri, enters
  the user_code, and signs in. Topaz could support this properly:
    1. GET /devicelogin — serves an HTML form that asks for user_code + username
    2. POST /devicelogin — looks up the device code by user_code, marks it authorized
       for the submitted user (writes into DeviceCodeEndpoint.AuthorizedDeviceCodes)
    3. Token polling returns authorization_pending until step 2 completes
  This would make the device code flow fully Azure-compatible and allow specifying any
  registered user when logging in via az login --use-device-code.
  milestone: v1.7-beta
  labels: enhancement, entra
-->

### Entra — ROPC login on non-container installs

_Implemented in v1.6-beta: a built-in HTTP CONNECT proxy starts on port 44380 alongside the Topaz host. MSAL's user-realm discovery pre-flight (`GET .../common/userrealm/{user}?api-version=1.0`) targets port 443 (stripped by MSAL from the authority URL); the proxy remaps the tunnel to port 8899 where Kestrel handles it. Set `HTTPS_PROXY=http://127.0.0.1:44380` before running `az login --username --password` on non-Docker local installs. Docker installs continue to bind port 443 directly and do not require the proxy._

---

## v1.7-beta

### Virtual Network — Private Endpoint IP tracking

<!--
TODO: Virtual Networks: Private Endpoint IP tracking
  Extend the IP allocation registry (introduced in v1.5-beta for NICs) to also record
  IP addresses assigned to Private Endpoints on creation. This requires implementing the
  Private Endpoint control plane (PUT/GET/DELETE/LIST for Microsoft.Network/privateEndpoints)
  and hooking it into IpAllocationRegistry.Register on create and IpAllocationRegistry.Unregister
  on delete, using the same subnetId → vnet resolution already used for NICs.
  milestone: v1.7-beta
  labels: enhancement, virtual-network, good first issue
-->

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

### Storage — SAS source IP (`sip`) enforcement

<!--
TODO: Storage: Service SAS sip (source IP) parameter enforcement
  Real Azure validates the `sip` parameter in a Service SAS token against the source IP
  of the incoming request, rejecting out-of-range callers with 403 AuthorizationSourceIPMismatch.
  Topaz currently detects the `sip` parameter and logs it at debug level but does not block
  any requests based on it.
  Required changes:
  - Extract the remote IP from HttpContext in the SAS validation layer (ServiceSasValidator).
  - Parse the `sip` value: support single IPv4/IPv6 addresses and hyphenated ranges
    (e.g. "192.168.0.1-192.168.0.255").
  - Compare the request source IP against the declared range; if it falls outside the range,
    return 403 with error code AuthorizationSourceIPMismatch.
  - Applies to all three storage services: Blob, Queue, and Table.
  See also: website/docs/known-limitations.md — "Storage SAS — sip (source IP) parameter not enforced".
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

### Azure Load Balancer — initial control plane

<!--
TODO: Azure Load Balancer: New service project scaffold
  Create Topaz.Service.LoadBalancer following the existing service conventions:
  - LoadBalancerResourceProperties + LoadBalancerResource (ArmResource<T>) with fields:
    sku (name: Basic/Standard, tier: Regional/Global), frontendIPConfigurations,
    backendAddressPools, loadBalancingRules, probes, inboundNatRules, outboundRules,
    provisioningState (always Succeeded).
  - LoadBalancerResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - LoadBalancerServiceControlPlane implementing IControlPlane with a working Deploy()
    that maps GenericResource → LoadBalancerResource via resource.As<T,TProps>().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj.
  - case "Microsoft.Network/loadBalancers": entry in TemplateDeploymentOrchestrator.RouteDeployment().
  See: https://learn.microsoft.com/en-us/rest/api/load-balancer/load-balancers?view=rest-load-balancer-2025-05-01
  milestone: v1.7-beta
  labels: enhancement, good first issue
-->

<!--
TODO: Azure Load Balancer: Control plane endpoints
  Implement the ARM-level Load Balancer resource surface (Microsoft.Network/loadBalancers):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}  – update tags
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Network/loadBalancers                              – list all in subscription
  The emulated load balancer does not route real traffic. All structural properties
  (frontendIPConfigurations, backendAddressPools, loadBalancingRules, probes,
  inboundNatRules, outboundRules) are stored and round-tripped as-is.
  provisioningState is always Succeeded.
  milestone: v1.7-beta
  labels: enhancement
-->

### Service Bus — dead letter queues

<!--
TODO: Service Bus: Dead letter queue support
  Implement dead-letter queue semantics for queues and topic subscriptions.
  When a message's delivery count exceeds MaxDeliveryCount, or when a subscriber
  explicitly dead-letters a message, move it to the entity's dead-letter sub-queue
  (<queue>/$DeadLetterQueue or <topic>/Subscriptions/<sub>/$DeadLetterQueue).
  Required AMQP changes:
  - Handle Rejected and Released dispositions by incrementing the delivery count.
  - When MaxDeliveryCount is exceeded, route the message to the dead-letter sub-queue
    instead of returning it to the main queue.
  - Expose the dead-letter sub-queue as an addressable AMQP entity so SDK callers can
    create a receiver for <queue>/$DeadLetterQueue.
  milestone: v1.7-beta
  labels: enhancement, service-bus
-->

### Service Bus — message sessions

<!--
TODO: Service Bus: Message session support
  Implement AMQP session-based messaging for queues and subscriptions with
  requiresSession = true.
  - Allow setting requiresSession on queue/subscription create or update (persist the flag).
  - On the AMQP layer, accept session-filtered Attach frames (filter map containing
    com.microsoft:session-filter with a string or null session ID).
  - Track active session locks per entity; enforce at most one active receiver per session.
  - Expose RenewSessionLock and GetSessionState/SetSessionState via AMQP management link.
  milestone: v1.7-beta
  labels: enhancement, service-bus
-->

### Service Bus — topic filters and rules

<!--
TODO: Service Bus: Topic subscription filters and rules
  Implement correlation filters, SQL filters, and rule actions for topic subscriptions.
  ARM control plane:
  - PUT    .../topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}  – create or update
  - GET    .../topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}  – get
  - DELETE .../topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}  – delete
  - GET    .../topics/{topicName}/subscriptions/{subscriptionName}/rules             – list
  Data plane:
  - On message publish to a topic, evaluate each subscription's active rules against
    the message's system properties and application properties.
  - CorrelationRuleFilter: match on ContentType, CorrelationId, MessageId, ReplyTo,
    ReplyToSessionId, SessionId, Subject, To, and user application properties.
  - SqlRuleFilter: evaluate SQL-92 predicate against system and user properties.
  - SqlRuleAction: apply property mutations (SET, REMOVE) after filter match.
  - Only forward messages to a subscription when at least one rule matches (or the
    subscription's only rule is TrueRuleFilter).
  Also implement the Atom XML data-plane rule endpoints on ServiceBusServiceAdditionalEndpoint
  to support ServiceBusAdministrationClient rule CRUD from the .NET SDK.
  milestone: v1.7-beta
  labels: enhancement, service-bus
-->

### Service Bus — authorization rules and SAS keys

<!--
TODO: Service Bus: Authorization rules and SAS key management
  Implement per-namespace, per-queue, and per-topic authorization rules and key endpoints.
  Namespace-level endpoints:
  - PUT    .../namespaces/{name}/authorizationRules/{ruleName}              – create or update
  - GET    .../namespaces/{name}/authorizationRules/{ruleName}              – get
  - DELETE .../namespaces/{name}/authorizationRules/{ruleName}              – delete
  - GET    .../namespaces/{name}/authorizationRules                         – list
  - POST   .../namespaces/{name}/authorizationRules/{ruleName}/listKeys     – list keys
  - POST   .../namespaces/{name}/authorizationRules/{ruleName}/regenerateKeys – regenerate
  Queue and Topic level: same pattern under /queues/{name}/... and /topics/{name}/...
  Persist SAS key pairs inside the authorization rule model; generate 256-bit random
  primary and secondary key pairs on rule creation.
  milestone: v1.7-beta
  labels: enhancement, service-bus
-->

---

## v1.8-preview

### ARM Deployments — deployment operations tracking

<!--
TODO: ARM Deployments: Deployment Operations — Get at resource group and subscription scope
  Implement the individual-operation GET endpoints for resource-group and subscription scopes:
  - GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Resources/deployments/{name}/operations/{operationId}
  - GET /subscriptions/{sub}/providers/Microsoft.Resources/deployments/{name}/operations/{operationId}
  Requires per-resource operation tracking in TemplateDeploymentOrchestrator: generate a GUID
  operationId per resource provision step and persist OperationRecord objects alongside the
  deployment resource file. The existing list endpoints can then read from the same store.
  milestone: v1.8-preview
  labels: enhancement
-->

<!--
TODO: ARM Deployments: Deployment Operations — List and Get at management group scope
  Implement deployment operations endpoints for management-group-scope deployments:
  - GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{name}/operations
  - GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{name}/operations/{operationId}
  Follows the same pattern as the existing resource-group and subscription scope list endpoints.
  Prerequisite: operation tracking work from the resource-group/subscription scope item above.
  milestone: v1.8-preview
  labels: enhancement
-->

<!--
TODO: ARM Deployments: Deployment Operations — List and Get at tenant scope
  Implement deployment operations endpoints for tenant-scope deployments:
  - GET /providers/Microsoft.Resources/deployments/{name}/operations
  - GET /providers/Microsoft.Resources/deployments/{name}/operations/{operationId}
  Follows the same pattern as the existing resource-group and subscription scope list endpoints.
  Prerequisite: operation tracking work from the resource-group/subscription scope item above.
  milestone: v1.8-preview
  labels: enhancement
-->

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

### Chaos Engineering — controllable fault injection

<!--
TODO: Chaos Engineering: Global chaos mode toggle and CLI command
  Add a global chaos-mode on/off switch to Topaz that can be toggled at runtime via the
  `topaz chaos` CLI command and via a REST control-plane endpoint:
  - `topaz chaos enable`  — activate chaos mode
  - `topaz chaos disable` — deactivate chaos mode
  - `topaz chaos status`  — print current mode and active fault rules as a table
  REST endpoint (on DefaultResourceManagerPort):
  - POST /topaz/chaos/enable
  - POST /topaz/chaos/disable
  - GET  /topaz/chaos/status  — returns {"enabled": true, "rules": [...]}
  Chaos state is held in-memory (not persisted across restarts).
  milestone: v1.8-preview
  labels: enhancement, chaos-engineering, good first issue
-->

<!--
TODO: Chaos Engineering: Fault rule configuration
  Allow users to define per-service (or global) fault rules that are evaluated on each
  incoming request when chaos mode is enabled.
  A fault rule has the following fields:
  - serviceNamespace (e.g. "Microsoft.Storage", "Microsoft.KeyVault", or "*" for all)
  - faultType: one of Timeout | TransientError | Throttle | ServiceUnavailable
  - faultRate: 0.0–1.0 (probability of injecting the fault for a matching request)
  - httpStatusCode: override for the response status code (e.g. 429, 500, 503)
  REST endpoints (on DefaultResourceManagerPort):
  - PUT  /topaz/chaos/rules/{ruleId} — create or replace a rule
  - GET  /topaz/chaos/rules/{ruleId} — get a rule
  - DELETE /topaz/chaos/rules/{ruleId} — delete a rule
  - GET  /topaz/chaos/rules — list all rules
  Rules are evaluated in registration order; first matching rule wins.
  milestone: v1.8-preview
  labels: enhancement, chaos-engineering
-->

<!--
TODO: Chaos Engineering: Router-level fault injection middleware
  Integrate fault injection into the Topaz router pipeline so that when chaos mode is
  enabled and a fault rule matches an incoming request, the router injects the configured
  fault before dispatching to the endpoint handler:
  - Timeout fault: delay the response by a configurable duration (default 30 s) before
    returning 408 Request Timeout or simply dropping the connection.
  - TransientError fault: return 500 Internal Server Error with a stock Azure-style JSON
    error body ({ "error": { "code": "InternalServerError", "message": "..." } }).
  - Throttle fault: return 429 Too Many Requests with a Retry-After header (default 5 s).
  - ServiceUnavailable fault: return 503 Service Unavailable.
  The faultRate field is respected: generate a random float per request; only inject
  the fault when the value is below faultRate (so 0.1 = 10% of requests are faulted).
  Log every injected fault at Information level so tests can observe it.
  milestone: v1.8-preview
  labels: enhancement, chaos-engineering
-->

### Azure App Configuration — initial control plane and data plane

<!--
TODO: Azure App Configuration: New service project scaffold
  Create Topaz.Service.AppConfiguration following existing service conventions:
  - ConfigurationStoreResourceProperties + ConfigurationStoreResource (ArmResource<T>)
    capturing: sku (Free/Standard), provisioningState (always Succeeded), endpoint
    (https://{name}.azconfig.topaz.local.dev:<AppConfigPort>/), publicNetworkAccess,
    disableLocalAuth, createMode, softDeleteRetentionInDays, enablePurgeProtection.
  - ConfigurationStoreResourceProvider (ResourceProviderBase<T>) for filesystem persistence
    under .topaz/app-configuration/{subscriptionId}/{resourceGroup}/{storeName}/.
  - AppConfigurationServiceControlPlane implementing IControlPlane with a working Deploy()
    that maps GenericResource → ConfigurationStoreResource via resource.As<T,TProps>().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "Microsoft.AppConfiguration/configurationStores": entry in
    TemplateDeploymentOrchestrator.RouteDeployment().
  - Add GlobalSettings.DefaultAppConfigurationPort constant (8896).
  See: https://learn.microsoft.com/en-us/rest/api/appconfiguration/
  milestone: v1.8-preview
  labels: enhancement, app-configuration, good first issue
-->

<!--
TODO: Azure App Configuration: ConfigurationStore control plane endpoints
  Implement the ARM-level ConfigurationStore resource surface
  (Microsoft.AppConfiguration/configurationStores):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.AppConfiguration/configurationStores/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.AppConfiguration/configurationStores/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.AppConfiguration/configurationStores/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.AppConfiguration/configurationStores/{name}  – update (tags, sku, publicNetworkAccess)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.AppConfiguration/configurationStores          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.AppConfiguration/configurationStores                              – list all
  Also implement access key management:
  - POST .../configurationStores/{name}/listKeys              – return primary and secondary read-write and read-only keys
  - POST .../configurationStores/{name}/regenerateKey         – regenerate one key by id
  Generate and persist two read-write and two read-only key pairs (id, value, connectionString)
  on first creation.
  milestone: v1.8-preview
  labels: enhancement, app-configuration, good first issue
-->

<!--
TODO: Azure App Configuration: Data plane — key-value store API
  Implement the App Configuration data-plane REST API on DefaultAppConfigurationPort (8896).
  Authentication: validate the HMAC-SHA256 credential scheme used by the Azure SDK
  (Authorization: HMAC-SHA256 Credential={keyId}&SignedHeaders=...&Signature=...).
  Endpoints:
  - GET    /kv                          – list key-values; support ?key=, ?label=, ?after= (pagination), $select= (field projection)
  - GET    /kv/{key}                    – get a single key-value (optional ?label= qualifier)
  - PUT    /kv/{key}                    – create or update a key-value (body: {"value":"...","contentType":"...","tags":{}})
  - DELETE /kv/{key}                    – delete a key-value (optional ?label=)
  - GET    /labels                      – list all labels in the store
  - GET    /revisions                   – list key-value change history (simplified: return current values only)
  - PUT    /locks/{key}                 – lock (read-only) a key-value
  - DELETE /locks/{key}                 – unlock a key-value
  Key-values are persisted as JSON files under the store's data directory.
  Support content types: plain string, application/json, application/vnd.microsoft.appconfig.ff+json
  (feature flags). Feature flag key-values use the key prefix .appconfig.featureflag/.
  Return ETag and Last-Modified headers on every key-value response.
  milestone: v1.8-preview
  labels: enhancement, app-configuration
-->

<!--
TODO: Azure App Configuration: MCP Server provisioning tool
  Extend Topaz.MCP with an App Configuration provisioning tool:
  - CreateAppConfigurationStore — create a ConfigurationStore in a resource group and
    return the endpoint URL and primary read-write connection string.
  Extend GetConnectionStrings to include the App Configuration connection string for
  provisioned stores.
  milestone: v1.8-preview
  labels: enhancement, app-configuration, mcp
-->

---

## v1.9-preview

### Application Insights — initial control plane and ingestion

<!--
TODO: Application Insights: New service project scaffold
  Create Topaz.Service.ApplicationInsights following existing service conventions:
  - ApplicationInsightsComponentResourceProperties + ApplicationInsightsComponentResource
    (ArmResource<T>) capturing: applicationType (web/other), kind (web/ios/other),
    flowType, requestSource, instrumentationKey (generated GUID), connectionString
    (InstrumentationKey=...;IngestionEndpoint=...;LiveEndpoint=...),
    provisioningState (always Succeeded), ingestionMode.
  - ApplicationInsightsResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - ApplicationInsightsServiceControlPlane implementing IControlPlane with a working Deploy().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "microsoft.insights/components": entry in TemplateDeploymentOrchestrator.RouteDeployment().
  - Add GlobalSettings.DefaultApplicationInsightsPort constant (8897).
  See: https://learn.microsoft.com/en-us/rest/api/application-insights/
  milestone: v1.9-preview
  labels: enhancement, application-insights, good first issue
-->

<!--
TODO: Application Insights: Component control plane endpoints
  Implement the ARM-level component resource surface (microsoft.insights/components):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/microsoft.insights/components/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/microsoft.insights/components/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/microsoft.insights/components/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/microsoft.insights/components/{name}  – update (tags, RetentionInDays, publicNetworkAccessForIngestion)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/microsoft.insights/components          – list by resource group
  - GET    /subscriptions/{sub}/providers/microsoft.insights/components                              – list all
  The instrumentationKey and connectionString fields are generated on first creation and
  persisted; they remain stable across updates.
  milestone: v1.9-preview
  labels: enhancement, application-insights, good first issue
-->

<!--
TODO: Application Insights: Telemetry ingestion endpoint
  Implement the Application Insights ingestion endpoint on DefaultApplicationInsightsPort
  (8897) that accepts telemetry items from the Azure Monitor OpenTelemetry SDK and the
  classic Application Insights SDK:
  - POST /v2/track — accepts a JSON array of telemetry envelopes (RequestData, TraceData,
    ExceptionData, EventData, MetricData, DependencyData); responds 200 with
    {"itemsReceived":N,"itemsAccepted":N,"errors":[]}.
  Received envelopes are persisted to disk under
  .topaz/application-insights/{instrumentationKey}/telemetry/{date}/{type}/{id}.json so
  they can be queried via the query endpoint.
  Map the incoming instrumentationKey (from the iKey field or the Authorization header)
  to the correct component resource.
  milestone: v1.9-preview
  labels: enhancement, application-insights
-->

<!--
TODO: Application Insights: Basic query API
  Implement a minimal subset of the Application Insights Query API so that
  `az monitor app-insights query` and the Azure SDK QueryClient work against Topaz:
  - POST /v1/apps/{instrumentationKey}/query — accepts {"query":"...","timespan":"..."};
    evaluates a simplified KQL-like query over the persisted telemetry envelopes.
  Supported table names: requests, traces, exceptions, customEvents, customMetrics,
  dependencies.
  Supported operators: where (equality, contains, startswith), project, summarize count(),
  order by timestamp desc, take N.
  Return the standard {"tables":[{"name":"PrimaryResult","columns":[...],"rows":[...]}]}
  schema.
  milestone: v1.9-preview
  labels: enhancement, application-insights
-->

### Log Analytics — initial control plane and ingestion

<!--
TODO: Log Analytics: New service project scaffold
  Create Topaz.Service.LogAnalytics following existing service conventions:
  - WorkspaceResourceProperties + WorkspaceResource (ArmResource<T>) capturing:
    sku (PerGB2018/Free/CapacityReservation), retentionInDays, workspaceId (GUID),
    customerId (same GUID, used for querying), provisioningState (always Succeeded),
    publicNetworkAccessForIngestion, publicNetworkAccessForQuery.
  - WorkspaceResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - LogAnalyticsServiceControlPlane implementing IControlPlane with a working Deploy().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "Microsoft.OperationalInsights/workspaces": entry in
    TemplateDeploymentOrchestrator.RouteDeployment().
  See: https://learn.microsoft.com/en-us/rest/api/loganalytics/
  milestone: v1.9-preview
  labels: enhancement, log-analytics, good first issue
-->

<!--
TODO: Log Analytics: Workspace control plane endpoints
  Implement the ARM-level workspace resource surface
  (Microsoft.OperationalInsights/workspaces):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces/{name}  – update (tags, retentionInDays, sku)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.OperationalInsights/workspaces                              – list all
  The workspaceId and customerId fields are generated as GUIDs on first creation and
  persist across updates.
  milestone: v1.9-preview
  labels: enhancement, log-analytics, good first issue
-->

<!--
TODO: Log Analytics: Data Collection (Logs Ingestion API)
  Implement the Logs Ingestion API endpoint (Azure Monitor Data Collection):
  - POST https://{workspaceId}.ods.opinsights.topaz.local.dev/api/logs?api-version=2016-04-01
    Body: JSON array of log records; header Log-Type sets the custom table name.
  Persists each batch of records to .topaz/log-analytics/{workspaceId}/{tableName}/{date}/{id}.json.
  Respond 200 with an empty body on success, matching real Azure behaviour.
  milestone: v1.9-preview
  labels: enhancement, log-analytics
-->

<!--
TODO: Log Analytics: Query API
  Implement a minimal KQL query endpoint so that `az monitor log-analytics query` and the
  Azure SDK LogsQueryClient work against Topaz:
  - POST /v1/workspaces/{workspaceId}/query — body: {"query":"...","timespan":"..."}
  Supported tables: custom tables ingested via the Logs Ingestion API, plus built-in
  aliases: AzureActivity, AzureDiagnostics (both always return empty results for
  resources not linked to the workspace).
  Supported KQL operators: where (==, contains, startswith, between), project, extend,
  summarize count()/sum()/avg()/min()/max() by, order by, take, union.
  Return the standard {"tables":[{"name":"PrimaryResult","columns":[...],"rows":[...]}]}
  schema matching the Log Analytics REST contract.
  milestone: v1.9-preview
  labels: enhancement, log-analytics
-->

<!--
TODO: Storage Account — geo-replication sync simulation
  Simulate the RA-GRS/RA-GZRS secondary-region replication lag lifecycle with a background scheduler.
  Work required:
  - Add a LastGeoSyncTime (DateTimeOffset?) field to StorageAccountResourceProperties; set to
    UtcNow - 30s on RA-GRS/RAGZRS account creation; null for non-geo-replicated SKUs.
  - Implement GeoReplicationSyncScheduler : ITopazBackgroundService that iterates all RA-GRS/RAGZRS
    accounts on a configurable periodic timer (default 30 s), updates LastGeoSyncTime = UtcNow,
    and persists via StorageResourceProvider. Follow the KeyVaultSecretsSoftDeletePurgeScheduler pattern.
  - Thread the persisted LastGeoSyncTime through GetBlobServiceStatsXml / GetQueueServiceStatsXml /
    GetTableServiceStatsXml so the <LastSyncTime> element in stats responses reflects the scheduler
    tick rather than the current wall-clock time.
  - Register GeoReplicationSyncScheduler in Topaz.Host/Host.cs alongside other background services.
  milestone: v1.9-preview
  labels: enhancement, storage
-->

---

## v1.10-preview

<!--
TODO: Azure API Management: New service project scaffold
  Create Topaz.Service.ApiManagement following existing service conventions:
  - ApiManagementServiceResourceProperties + ApiManagementServiceResource (ArmResource<T>)
    capturing: sku (Developer/Basic/Standard/Premium/Consumption), publisherEmail,
    publisherName, gatewayUrl (https://{name}.azure-api.topaz.local.dev:<ApiManagementPort>/),
    portalUrl, managementApiUrl, provisioningState (always Succeeded).
  - ApiManagementResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - ApiManagementServiceControlPlane implementing IControlPlane with a working Deploy().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "Microsoft.ApiManagement/service": entry in TemplateDeploymentOrchestrator.RouteDeployment().
  - Add GlobalSettings.DefaultApiManagementPort constant (8900).
  See: https://learn.microsoft.com/en-us/rest/api/apimanagement/
  milestone: v1.10-preview
  labels: enhancement, api-management, good first issue
-->

<!--
TODO: Azure API Management: Service control plane endpoints
  Implement the ARM-level ApiManagementService resource surface
  (Microsoft.ApiManagement/service):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ApiManagement/service/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ApiManagement/service/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ApiManagement/service/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ApiManagement/service/{name}  – update (tags, sku, publisherEmail)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ApiManagement/service          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.ApiManagement/service                              – list all
  provisioningState is always Succeeded. gatewayUrl, portalUrl, and managementApiUrl
  are derived from the service name and persisted on creation.
  milestone: v1.10-preview
  labels: enhancement, api-management
-->

<!--
TODO: Azure API Management: Data plane — APIs CRUD
  Implement ARM-level API resource endpoints under the service instance:
  - PUT    .../service/{name}/apis/{apiId}  – create or update an API definition
  - GET    .../service/{name}/apis/{apiId}  – get
  - DELETE .../service/{name}/apis/{apiId}  – delete
  - GET    .../service/{name}/apis          – list APIs
  An API definition includes: displayName, description, serviceUrl (backend target),
  path, protocols (http/https), apiType (http/soap/websocket/graphql).
  Persist API definitions as subresources. Includes E2E SDK tests and Azure CLI tests.
  milestone: v1.10-preview
  labels: enhancement, api-management
-->

<!--
TODO: Azure API Management: Data plane — Products CRUD
  Implement ARM-level Product resource endpoints:
  - PUT    .../service/{name}/products/{productId}  – create or update
  - GET    .../service/{name}/products/{productId}  – get
  - DELETE .../service/{name}/products/{productId}  – delete
  - GET    .../service/{name}/products              – list
  Products group APIs and are the unit of subscription. Fields: displayName, description,
  state (published/notPublished), subscriptionRequired, approvalRequired.
  Also implement product-API association: PUT/DELETE/GET .../products/{productId}/apis/{apiId}.
  milestone: v1.10-preview
  labels: enhancement, api-management
-->

<!--
TODO: Azure API Management: Data plane — Backends CRUD
  Implement ARM-level Backend resource endpoints:
  - PUT    .../service/{name}/backends/{backendId}  – create or update
  - GET    .../service/{name}/backends/{backendId}  – get
  - DELETE .../service/{name}/backends/{backendId}  – delete
  - GET    .../service/{name}/backends              – list
  A backend defines a target service URL and optional credentials/TLS settings.
  Fields: url, protocol (http/soap), description, title, resourceId.
  Backends are referenced by policy expressions and persisted as subresources.
  milestone: v1.10-preview
  labels: enhancement, api-management
-->

<!--
TODO: Azure API Management: Data plane — Policies CRUD
  Implement ARM-level Policy resource endpoints at service, API, and operation scope:
  - PUT    .../service/{name}/policies/policy         – service-level policy
  - GET    .../service/{name}/policies/policy
  - DELETE .../service/{name}/policies/policy
  - PUT    .../service/{name}/apis/{apiId}/policies/policy     – API-level policy
  - GET    .../service/{name}/apis/{apiId}/policies/policy
  - DELETE .../service/{name}/apis/{apiId}/policies/policy
  Policies are stored as raw XML strings (APIM policy document format). No policy
  execution is performed in v1.10; the emulator stores and returns the XML verbatim.
  Policy execution (inbound/outbound/backend/on-error) is planned for a future version.
  milestone: v1.10-preview
  labels: enhancement, api-management
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
