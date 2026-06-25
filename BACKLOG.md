# Backlog

Central place for planning upcoming work. Each TODO block below is automatically
converted to a GitHub Issue by CI when new lines are committed.

> **Note:** The action only picks up TODOs that appear in the *diff* of a new commit.
> To bulk-import existing items, run the _TODO to Issue_ workflow manually from the
> **Actions** tab.

---

<!--
TODO: Storage — Fix List Keys endpoint for Azure CLI show-connection-string
  `az storage account show-connection-string` (and the underlying automatic key-lookup
  path used by `az storage container create --account-name` without explicit credentials)
  returns 404 from Topaz. Investigate the exact ARM path and API version the Azure CLI
  sends for this call and ensure the existing List Keys handler matches it.
  Workaround until fixed: callers must pass --connection-string, --account-key, or
  --sas-token explicitly on all az storage data-plane commands.
  milestone: v1.8-preview
  labels: bug, storage
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

_Investigated in v1.6-beta: Phase 1 baseline established. AMQPNetLite's omission of trailing null fields is explicitly permitted by the AMQP 1.0 spec (section 1.4); the issue is that the real Azure broker always emits full-length frames, and several SDKs were written assuming that. Two Topaz application-level bugs were fixed during the investigation: missing `MessageId` on received messages (broke Node.js Service Bus auto-lock-renewal) and missing batch-message unwrapping for Event Hub send batches (broke Node.js Event Hubs receive). After those fixes the Phase 1 compatibility matrix is: .NET SDK — fully passes; Node.js SDK — fully passes without patches; Python `azure-servicebus` — passes with frame-padding patches in `conftest.py`; Python `azure-eventhub` — fails with `IndexError` in `_pyamqp`. Upgrading AMQPNetLite to 2.5.3 does not resolve the Python incompatibility. Phase 2 investigation (trailing-null padding) is tracked in v1.7-beta._

### ARM Deployments — mid-flight cancellation

_Implemented in v1.6-beta: cooperative cancellation via `CancellationTokenSource` per deployment. `CancelDeployment` now signals the token when the deployment is `Running`; `RouteDeployment` checks it after each resource provision and transitions `provisioningState` to `Canceled`, leaving already-provisioned resources in place. All four ARM scopes (resource group, subscription, tenant, management group) support mid-flight cancel. Existing 409 behaviour for already-completed deployments is preserved._

### Azure Storage — unified data-plane port

_Implemented in v1.6-beta: all storage data-plane sub-services (Blob, Table, Queue, File) now share a single HTTPS port (`DefaultStoragePort = 8891`). The Router gained a `RequiredHostServiceLabel` interface member so blob/table/queue endpoints co-exist on the same port and are disambiguated by the Host header subdomain (e.g. `{account}.blob.*`, `{account}.table.*`). The four old port constants are aliased to `DefaultStoragePort`. `GetMetadataEndpointResponse.Suffixes["storage"]` updated to `:8891`, removing the `ParseAccountID` port-matching workaround. `TopazResourceHelpers.GetAzureStorageConnectionString` updated; fixed `DefaultEndpointsProtocol=http` → `https` and missing trailing `/` on Table endpoint. All E2E, AzureCLI, and Terraform tests updated._

### ACE (Azure Cost Estimator) integration

_Implemented in v1.6-beta: `TheCloudTheory.AzureCostEstimator.Core` NuGet package extracted from ACE and consumed by `Topaz.FinOps`. Topaz-native endpoint `GET /topaz/subscriptions/{sub}/estimatedCosts` returns monthly cost estimates for all provisioned resources in a subscription; supports 17 currencies via optional `?currency=` query parameter. `topaz finops estimate` CLI command prints a formatted cost breakdown table or JSON. Portal Cost Analysis page deferred to v1.7-beta._

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

<!--
TODO: Azure Cosmos DB: SQL query — GROUP BY aggregates
  Extend the SQL query engine (v1.7-beta) to support GROUP BY on a single field:
  - SELECT c.category, COUNT(1) FROM c GROUP BY c.category
  - Partition filtered documents by the specified field before applying aggregate
    functions (COUNT, SUM, MIN, MAX, AVG) and return one result row per unique value.
  milestone: v1.9-preview
  labels: enhancement, cosmos-db
-->

<!--
TODO: Azure Cosmos DB: SQL query — ORDER BY on aggregate results
  Extend the SQL query engine (v1.7-beta) to support ORDER BY applied to aggregate
  output columns:
  - SELECT c.category, COUNT(1) AS cnt FROM c GROUP BY c.category ORDER BY cnt DESC
  - Prerequisite: GROUP BY support (above).
  milestone: v1.9-preview
  labels: enhancement, cosmos-db
-->

### Azure Disks — initial control plane

_Implemented in v1.6-beta: service scaffold — `DiskResourceProperties`, `DiskResource`, `DiskResourceProvider`, `DiskServiceControlPlane` (with working `Deploy()`), and `DiskService` registered in `Topaz.Host`. `Microsoft.Compute/disks` is wired into `TemplateDeploymentOrchestrator.RouteDeployment()`. Control-plane endpoints and SAS access endpoints are tracked separately below._

_Implemented in v1.6-beta: six control-plane HTTP endpoints (Create/Update, Get, Delete, PATCH update, List by resource group, List by subscription), five Topaz CLI commands (`topaz disk create/show/delete/update/list`), E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests._

_Implemented in v1.6-beta: `beginGetAccess` and `endGetAccess` endpoints with Topaz-hosted SAS stub URL (`https://topaz.local.dev:8899/disk-sas/{uniqueId}`), `GrantDiskAccessCommand` and `RevokeDiskAccessCommand` CLI commands, E2E SDK tests, Azure CLI tests, and Topaz CLI tests._

### Entra — AuthorizeEndpoint `response_mode=form_post`

_Implemented in v1.6-beta: `AuthorizeEndpoint` now reads `response_mode` from the query string. When `form_post` is requested, it returns `200 OK` with an HTML auto-submit form that POSTs `code` and `state` to `redirect_uri` instead of issuing a `302` redirect. This fixes compatibility with Azure CLI 2.86.0 (MSAL), which sets `response_mode=form_post` and rejects redirect responses with "response_mode=query is not supported"._

### Entra — Device Code flow (`POST /devicecode` + `grant_type=device_code`)

_Implemented in v1.6-beta: `DeviceCodeEndpoint` handles `POST /organizations|{tenantId}|common/oauth2/v2.0/devicecode` and returns a valid RFC 8628 device-authorization response (`device_code`, `user_code`, `verification_uri`, `expires_in`, `interval`, `message`). `TokenEndpoint` now handles `grant_type=urn:ietf:params:oauth:grant-type:device_code`. When a `login_hint` is present in the device code request the matching user is resolved; otherwise the global admin is used as a placeholder (see TODO below)._

### Entra — ROPC login on non-container installs

_Implemented in v1.6-beta: a built-in HTTP CONNECT proxy starts on port 44380 alongside the Topaz host. MSAL's user-realm discovery pre-flight (`GET .../common/userrealm/{user}?api-version=1.0`) targets port 443 (stripped by MSAL from the authority URL); the proxy remaps the tunnel to port 8899 where Kestrel handles it. Set `HTTPS_PROXY=http://127.0.0.1:44380` before running `az login --username --password` on non-Docker local installs. Docker installs continue to bind port 443 directly and do not require the proxy._

---

## v1.7-beta

### ARM Deployments — unhandled exception leaves deployment stuck in Running state

_Implemented in v1.7-beta: unhandled exceptions on the deployment background thread now transition the deployment to `provisioningState=Failed` instead of leaving it stuck at `Running`. Added an inner `try-catch(Exception)` in `HandleNestedDeployment` around `RouteDeployment(innerJob)` — sets `DeploymentErrorInfo { Code="DeploymentFailed", Message=ex.Message }` on the nested `DeploymentResource`, calls `Fail()` and `Persist()`. Added a secondary safety-net `catch` in `Start()` for top-level deployments. `DeploymentResourceProperties.Error` changed from `Azure.ResponseError` (not STJ-serializable) to a new `DeploymentErrorInfo` POCO. Includes two E2E regression tests in `ArmDeploymentFailureTests.cs` using template `deployment-nested-reference-output.json`._



### Azure Cosmos DB — MCP Server provisioning tools

_Implemented in v1.7-beta: three MCP provisioning tools (`CreateCosmosDbAccount`, `CreateCosmosDbDatabase`, `CreateCosmosDbContainer`) for SQL API account/database/container creation with partition key support. `GetConnectionStrings` extended to enumerate and return Cosmos DB accounts with AccountEndpoint and PrimaryConnectionString. Full test coverage (8 account/database/container provisioning tests + 2 connection string integration tests)._

### VirtualNetwork — Public IP Address (PIP) CRUD

_Implemented in v1.7-beta: six control-plane endpoints (Create/Update, Get, Delete, List by resource group, List by subscription, Update Tags) for `Microsoft.Network/publicIPAddresses`. Stub IP address assigned from RFC 5737 documentation range (203.0.113.0/24) on creation. Full `Deploy()` support for ARM template deployments. Four Topaz CLI commands (`topaz pip create/show/delete/list`). Includes E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests._

### Azure App Service — Kudu / SCM data plane

_Implemented in v1.7-beta: `POST /api/zipdeploy` and `GET /api/deployments` on a dedicated Kudu port (8896, HTTPS). Site identity is resolved from the `Host` header (`{siteName}.scm.azurewebsites.topaz.local.dev`). Zip packages are stored at `.topaz/{sub}/{rg}/.azure-web-sites/{name}/deployments/{id}.zip`; a `DeploymentRecord` (id, status, startTime, endTime, deployer: `"Push Deployer"`) is persisted at `.../deployments/{id}/metadata.json`. Certificate SAN `*.scm.azurewebsites.topaz.local.dev` added to `certificate/generate.sh` and cert regenerated._

### Service Bus — authorization rules and SAS keys

Implemented in v1.7-beta. Per-namespace, per-queue, and per-topic authorization rules with full CRUD,
listKeys, and regenerateKeys. 256-bit random key pairs generated on rule creation. `RootManageSharedAccessKey`
auto-created on namespace creation.
<!--
  milestone: v1.7-beta
  labels: enhancement, service-bus
-->

### AMQP — trailing null padding in AMQPNetLite performatives

<!--
TODO: AMQP: Investigate patching AMQPNetLite to emit full-length performatives
  Phase 1 (v1.6-beta) confirmed that AMQPNetLite omits trailing null fields in encoded
  performatives, which is AMQP 1.0 spec-compliant but breaks non-.NET clients whose
  decoders access fields by fixed numeric index (specifically Python _pyamqp used by
  azure-eventhub 5.x and azure-servicebus via uamqp). Upgrading to AMQPNetLite 2.5.3
  does not resolve this.

  Investigate whether AMQPNetLite can be patched — either via a subclass override or a
  post-encode buffer rewrite — to append trailing null bytes to every performative so
  the encoded frame length matches the full field count defined by the AMQP 1.0 spec
  for each performative type (Open, Begin, Attach, Transfer, Flow, Disposition, Detach,
  End, Close). The goal is to make Topaz's output match what the real Azure broker emits
  without replacing the entire AMQP stack.

  If patching AMQPNetLite is not feasible (e.g. frames are sealed before any
  post-processing hook is reachable), evaluate replacing AMQPNetLite with a fully
  spec-compliant server implementation (e.g. Apache Qpid Proton .NET or a custom minimal
  AMQP 1.0 server) that always encodes explicit nulls for every defined field.

  Success criterion: Python azure-eventhub and azure-servicebus tests pass without any
  monkey-patching in conftest.py.

  See also: website/docs/known-limitations.md — "AMQP — AMQPNetLite encoding breaks
  non-.NET clients".
  milestone: v1.7-beta
  labels: enhancement, service-bus, event-hub, amqp
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

### Azure App Service — Kudu SCM — individual deployment GET endpoint

<!--
TODO: Azure App Service: Kudu SCM — individual deployment GET endpoint
  Implement the single-deployment lookup endpoint for the Kudu/SCM data-plane
  (prerequisite: Kudu SCM data plane from v1.7-beta).
  - GET /api/deployments/{id}
    Parse siteName from the Host header ({siteName}.scm.azurewebsites.topaz.local.dev).
    Read .topaz/{sub}/{rg}/.azure-web-sites/{name}/deployments/{id}/metadata.json
    and return the DeploymentRecord as JSON (200 OK), or 404 if the deployment ID
    does not exist for the resolved site.
  Add GetDeploymentByIdEndpoint.cs under Services/Topaz.Service.AppService/Endpoints/Kudu/
  and register it in AppServiceKuduService.Endpoints.
  Includes E2E test and Azure CLI test.
  milestone: v1.8-preview
  labels: enhancement, app-service
-->

### Chaos Engineering — controllable fault injection

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

### Azure Disks — SAS access LRO polling

<!--
TODO: Azure Disks: SAS access — LRO polling for beginGetAccess
  Upgrade beginGetAccess from synchronous 200 OK to a proper long-running operation:
  - POST .../disks/{name}/beginGetAccess returns 202 Accepted with Azure-AsyncOperation header.
  - GET on polling URL returns {"status":"InProgress"} then {"status":"Succeeded","properties":{"output":{"accessSAS":"..."}}}
  - Implement polling URL as new endpoint on DefaultResourceManagerPort.
  - Store pending LRO state in-memory per disk; garbage-collect after completion.
  - The accessSAS value remains the Topaz-hosted stub URL.
  milestone: v1.8-preview
  labels: enhancement
-->

---

## v1.9-preview

### Azure App Service — Kudu / SCM authentication

<!--
TODO: Azure App Service: Kudu SCM — publishing credentials and Basic auth
  Implement per-site publishing credentials and Basic auth enforcement for the
  Kudu/SCM data-plane (prerequisite: Kudu SCM data plane from v1.7-beta).
  Publishing credentials:
  - Generate a userName (${siteName}\$publishinguser format) and a random 44-character
    password on site creation; persist as part of AppServiceSiteResourceProperties
    (or as a dedicated sub-resource).
  - Expose via ARM endpoint:
    POST .../sites/{name}/listPublishingCredentials — returns userName, password, scmUri.
  Kudu Basic auth enforcement:
  - Validate Authorization: Basic base64({userName}:{password}) on all Kudu endpoints.
  - Return 401 Unauthorized with WWW-Authenticate: Basic realm="Kudu" when credentials
    are absent or incorrect.
  milestone: v1.9-preview
  labels: enhancement, app-service, security
-->

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

### Azure Disks — full disk data streaming (azcopy)

<!--
TODO: Azure Disks: Full azcopy-compatible disk streaming via SAS URL
  - On beginGetAccess, Topaz stores sparse byte store keyed by uniqueId.
  - GET /disk-sas/{uniqueId} honours HTTP Range requests.
  - PUT to same URL accepts byte-range uploads with page-blob headers.
  - HEAD reports Content-Length = diskSizeGB * 1024^3.
  - endGetAccess clears the byte store and sets diskState back to Unattached.
  - Large disks use on-disk .topaz/disks/{uniqueId}.vhd sparse file.
  milestone: v1.9-preview
  labels: enhancement
-->

<!--
TODO: Service Bus: Dead letter queue — DeadLetteringOnMessageExpiration
  Automatically move messages to the dead-letter sub-queue when their TTL expires.
  When a message's TTL (TimeToLive on the message, or DefaultMessageTimeToLive on the
  entity) elapses while the message is sitting in the queue, it should be routed to
  <entity>/$DeadLetterQueue with DeadLetterReason="TTLExpiredException" instead of being
  silently discarded.
  Prerequisite: dead-letter queue support (v1.7-beta).
  Required changes:
  - Add a background scheduler (ITopazBackgroundService) that periodically scans
    SubscriptionMessageStore for messages whose x-opt-enqueued-time-utc + TTL < UtcNow.
  - Expired messages are removed from the main queue and enqueued in
    <entity>/$deadletterqueue with the appropriate dead-letter annotations.
  - Respect DeadLetteringOnMessageExpiration=false (default) per entity — when false,
    expired messages are simply discarded rather than dead-lettered.
  milestone: v1.9-preview
  labels: enhancement, service-bus
-->

<!--
TODO: Service Bus: Dead letter queue — ForwardDeadLetteredMessagesTo
  When a queue or topic subscription has ForwardDeadLetteredMessagesTo set to another
  entity name, automatically forward messages that land in the DLQ to the target entity
  instead of keeping them in <entity>/$deadletterqueue.
  Prerequisite: dead-letter queue support (v1.7-beta).
  Required changes:
  - Read ForwardDeadLetteredMessagesTo from ServiceBusQueueResourceProperties /
    ServiceBusSubscriptionResourceProperties when a message is routed to the DLQ.
  - If the property is set, enqueue the message to the target entity's main queue
    (resolve via ServiceBusRuleLoader or SubscriptionMessageStore) instead of
    <entity>/$deadletterqueue.
  - If the forwarding target does not exist, fall back to the local DLQ.
  milestone: v1.9-preview
  labels: enhancement, service-bus
-->

<!--
TODO: Service Bus: Dead letter queue — session-filtered DLQ access
  Allow SDK callers to receive dead-lettered messages from session-enabled entities via a
  session-filtered receiver on the DLQ address.
  Prerequisite: dead-letter queue support (v1.7-beta), message session support.
  Required changes:
  - When a message is dead-lettered from a session-enabled queue or subscription, preserve
    the SessionId annotation on the DLQ message.
  - Accept session-filtered Attach frames (com.microsoft:session-filter) on
    <entity>/$DeadLetterQueue addresses, applying the same session-lock semantics as the
    main queue's session receiver (see Message session support backlog item).
  milestone: v1.9-preview
  labels: enhancement, service-bus
-->

<!--
TODO: Storage Account — geo-replication sync simulation
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

<!--
TODO: Virtual Machines: Complete Microsoft.Compute/skus SKU catalogue
  The current `GET /subscriptions/{sub}/providers/Microsoft.Compute/skus` stub returns only 17
  general-purpose VM SKUs. Real Azure returns 200–400+ SKUs per region covering all families
  (A, B, D, E, F, G, L, M, N, etc.). ACE's CapabilitiesCache looks up each template VM SKU
  in this list; a cache miss causes ACE to assume no PremiumIO support and may produce an
  incorrect disk-cost estimate for that VM.
  Required changes:
  - Replace the hard-coded 17-entry array in ListComputeResourceSkusEndpoint with a full
    static catalogue sourced from `az vm list-skus --location eastus -o json` (or equivalent
    Azure REST API snapshot). Include at minimum all SKU families that appear in Terraform
    community templates: A, B, D, Ds, E, Es, F, Fs, G, L, M, N (GPU), and their _v2/_v3/_v4
    variants.
  - The location parameter from the `$filter=location eq '...'` query string must be threaded
    through to the response so each SKU's `locations` array reflects the requested region.
  - Add an E2E test that verifies the endpoint returns a non-empty list and that at least one
    entry has `resourceType == "virtualMachines"` and a `PremiumIO` capability.
  milestone: v1.9-preview
  labels: enhancement, virtual-machine, finops
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

### Azure Container Instances — initial control plane

<!--
TODO: Azure Container Instances: New service project scaffold
  Create Topaz.Service.ContainerInstances following existing service conventions:
  - ContainerGroupResourceProperties + ContainerGroupResource (ArmResource<T>) capturing:
    containers (array of ContainerDefinition with name, image, resources, ports, environmentVariables,
    volumeMounts), osType (Linux/Windows), restartPolicy (Always/OnFailure/Never),
    ipAddress (type: Public/Private, ports, ip), provisioningState (always Succeeded),
    instanceView (state: Running), volumes.
  - ContainerGroupResourceProvider (ResourceProviderBase<T>) for filesystem persistence
    under .topaz/container-instances/{subscriptionId}/{resourceGroup}/{groupName}/.
  - ContainerInstancesServiceControlPlane implementing IControlPlane with a working Deploy()
    that maps GenericResource → ContainerGroupResource via resource.As<T,TProps>().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "Microsoft.ContainerInstance/containerGroups": entry in
    TemplateDeploymentOrchestrator.RouteDeployment().
  See: https://learn.microsoft.com/en-us/rest/api/container-instances/operation-groups?view=rest-container-instances-2025-09-01
  milestone: v1.10-preview
  labels: enhancement, container-instances, good first issue
-->

<!--
TODO: Azure Container Instances: Container Groups control plane endpoints
  Implement the ARM-level ContainerGroup resource surface
  (Microsoft.ContainerInstance/containerGroups):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ContainerInstance/containerGroups/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ContainerInstance/containerGroups/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ContainerInstance/containerGroups/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ContainerInstance/containerGroups/{name}  – update (tags)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ContainerInstance/containerGroups          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.ContainerInstance/containerGroups                              – list by subscription
  provisioningState is always Succeeded. instanceView.state is always Running.
  ipAddress.ip is a stub value (e.g. "10.0.0.1") assigned on creation.
  Includes E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests.
  milestone: v1.10-preview
  labels: enhancement, container-instances
-->

<!--
TODO: Azure Container Instances: Container Groups lifecycle endpoints
  Implement lifecycle operation endpoints for container groups:
  - POST .../containerGroups/{name}/start   – start all containers (returns 204 No Content)
  - POST .../containerGroups/{name}/stop    – stop all containers (returns 204 No Content)
  - POST .../containerGroups/{name}/restart – restart all containers (returns 204 No Content)
  All three operations are no-ops in the emulator (no real containers are started or stopped).
  provisioningState and instanceView.state remain Succeeded/Running regardless of lifecycle calls.
  milestone: v1.10-preview
  labels: enhancement, container-instances
-->

<!--
TODO: Azure Container Instances: Containers data-plane endpoints (logs)
  Implement the Containers sub-resource endpoints:
  - GET .../containerGroups/{name}/containers/{containerName}/logs
    Returns a stub log body with a single line "Container emulated by Topaz."
    Supports optional ?tail= query parameter (integer); ignored in emulation.
  This satisfies `az container logs` calls without running real containers.
  milestone: v1.10-preview
  labels: enhancement, container-instances, good first issue
-->

### Availability Sets — initial control plane

<!--
TODO: Availability Sets: New service control plane
  Implement the ARM-level AvailabilitySet resource surface
  (Microsoft.Compute/availabilitySets) in the existing Topaz.Service.VirtualMachine project
  (or a new Topaz.Service.AvailabilitySets project if separation is preferred):
  - AvailabilitySetResourceProperties + AvailabilitySetResource (ArmResource<T>) capturing:
    sku (name: Aligned/Classic), platformUpdateDomainCount (default 5),
    platformFaultDomainCount (default 2), virtualMachines (list of sub-resource IDs),
    provisioningState (always Succeeded).
  - AvailabilitySetResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - Deploy() support in the control plane; register "Microsoft.Compute/availabilitySets"
    in TemplateDeploymentOrchestrator.RouteDeployment().
  Endpoints:
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/availabilitySets/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/availabilitySets/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/availabilitySets/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/availabilitySets/{name}  – update (tags, platformFaultDomainCount)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/availabilitySets          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Compute/availabilitySets                              – list by subscription
  - GET    .../availabilitySets/{name}/vmSizes  – list available VM sizes (return the same stub catalogue as ListComputeResourceSkusEndpoint)
  Includes E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests.
  See: https://learn.microsoft.com/en-us/rest/api/compute/availability-sets?view=rest-compute-2025-11-01
  milestone: v1.10-preview
  labels: enhancement, virtual-machine, good first issue
-->

### Private Endpoints — initial control plane

<!--
TODO: Virtual Networks: Private Endpoint IP tracking
  Extend the IP allocation registry (introduced in v1.5-beta for NICs) to also record
  IP addresses assigned to Private Endpoints on creation. This requires implementing the
  Private Endpoint control plane (PUT/GET/DELETE/LIST for Microsoft.Network/privateEndpoints)
  and hooking it into IpAllocationRegistry.Register on create and IpAllocationRegistry.Unregister
  on delete, using the same subnetId → vnet resolution already used for NICs.
  milestone: v1.10-preview
  labels: enhancement, virtual-network, good first issue
-->

<!--
TODO: Private Endpoints: New service control plane
  Implement the ARM-level PrivateEndpoint resource surface
  (Microsoft.Network/privateEndpoints) as an extension of the existing
  Topaz.Service.VirtualNetwork project:
  - PrivateEndpointResourceProperties + PrivateEndpointResource (ArmResource<T>) capturing:
    subnet (sub-resource ID reference), privateLinkServiceConnections (array with
    privateLinkServiceId, groupIds, privateLinkServiceConnectionState),
    networkInterfaces (list of auto-created NIC sub-resource IDs), provisioningState (always Succeeded),
    customDnsConfigs (array of {fqdn, ipAddresses}).
  - PrivateEndpointResourceProvider (ResourceProviderBase<T>) for filesystem persistence.
  - Deploy() support; register "Microsoft.Network/privateEndpoints" in
    TemplateDeploymentOrchestrator.RouteDeployment().
  - On creation, register the endpoint's IP (resolved from the linked subnet CIDR via
    IpAllocationRegistry) and unregister it on deletion — satisfying the Private Endpoint
    IP tracking backlog item from v1.7-beta.
  Endpoints:
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/privateEndpoints/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/privateEndpoints/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/privateEndpoints/{name}  – delete
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/privateEndpoints          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Network/privateEndpoints                              – list by subscription
  privateLinkServiceConnectionState is always set to {status: "Approved", description: "Auto-approved by Topaz"}.
  Includes E2E SDK tests, Azure CLI tests, and Terraform tests.
  See: https://learn.microsoft.com/en-us/rest/api/virtualnetwork/private-endpoints?view=rest-virtualnetwork-2025-05-01
  milestone: v1.10-preview
  labels: enhancement, virtual-network
-->

### Azure Redis Cache — initial control plane

<!--
TODO: Azure Redis Cache: New service project scaffold
  Create Topaz.Service.Redis following existing service conventions:
  - RedisResourceProperties + RedisResource (ArmResource<T>) capturing:
    sku (name: Basic/Standard/Premium, family: C/P, capacity: 0–6),
    redisVersion (default "6"), enableNonSslPort (default false),
    minimumTlsVersion (default "1.2"), replicasPerMaster, shardCount,
    hostName ({name}.redis.cache.topaz.local.dev), port (6379), sslPort (6380),
    accessKeys (primaryKey, secondaryKey — 44-byte random base64 strings generated on creation),
    provisioningState (always Succeeded), redisConfiguration.
  - RedisResourceProvider (ResourceProviderBase<T>) for filesystem persistence
    under .topaz/redis/{subscriptionId}/{resourceGroup}/{cacheName}/.
  - RedisServiceControlPlane implementing IControlPlane with a working Deploy()
    that maps GenericResource → RedisResource via resource.As<T,TProps>().
  - IServiceDefinition registration and wiring in Topaz.Host.
  - ProjectReference in Topaz.Service.ResourceManager.csproj and a
    case "Microsoft.Cache/redis": entry in TemplateDeploymentOrchestrator.RouteDeployment().
  See: https://learn.microsoft.com/en-us/rest/api/redis/operation-groups?view=rest-redis-2024-11-01
  milestone: v1.10-preview
  labels: enhancement, redis, good first issue
-->

<!--
TODO: Azure Redis Cache: Control plane endpoints
  Implement the ARM-level Redis cache resource surface (Microsoft.Cache/redis):
  - PUT    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Cache/redis/{name}  – create or update
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Cache/redis/{name}  – get
  - DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Cache/redis/{name}  – delete
  - PATCH  /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Cache/redis/{name}  – update (tags, sku, enableNonSslPort, minimumTlsVersion, redisConfiguration)
  - GET    /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Cache/redis          – list by resource group
  - GET    /subscriptions/{sub}/providers/Microsoft.Cache/redis                              – list by subscription
  - POST   .../redis/{name}/listKeys       – return primary and secondary access keys
  - POST   .../redis/{name}/regenerateKey  – regenerate the specified key (keyType: Primary/Secondary)
  Access keys are generated on first creation and persisted; regenerateKey replaces the specified key.
  GET responses must not include accessKeys inline (keys are only returned via listKeys).
  Includes E2E SDK tests, Azure CLI tests, Azure PowerShell tests, and Terraform tests.
  milestone: v1.10-preview
  labels: enhancement, redis
-->

<!--
TODO: Azure Redis Cache: Firewall Rules CRUD
  Implement per-cache firewall rule endpoints (Microsoft.Cache/redis/firewallRules):
  - PUT    .../redis/{name}/firewallRules/{ruleName}  – create or update (startIP, endIP)
  - GET    .../redis/{name}/firewallRules/{ruleName}  – get
  - DELETE .../redis/{name}/firewallRules/{ruleName}  – delete
  - GET    .../redis/{name}/firewallRules              – list
  Rules are persisted as subresources of the cache. No actual IP filtering is enforced in the emulator.
  milestone: v1.10-preview
  labels: enhancement, redis, good first issue
-->

<!--
TODO: Azure Redis Cache: MCP Server provisioning tool
  Extend Topaz.MCP with a Redis Cache provisioning tool:
  - CreateRedisCache — create a Redis cache in a resource group and return the
    hostName, sslPort, and primary access key.
  Extend GetConnectionStrings to include the Redis connection string for provisioned caches
  in the format: {hostName}:{sslPort},password={primaryKey},ssl=True,abortConnect=False
  milestone: v1.10-preview
  labels: enhancement, redis, mcp
-->

---

## v1.11

### Azure OpenAI Service — stub endpoint

<!--
TODO: Azure OpenAI Service: Stub chat completions endpoint
  Implement a configurable Azure OpenAI stub on DefaultResourceManagerPort (or a dedicated
  port, e.g. 8901) so that AzureOpenAIClient (.NET, Python, JS) works offline against Topaz.
  Endpoint:
  - POST /openai/deployments/{deploymentName}/chat/completions?api-version={version}
    Reads a per-deployment response fixture from
    .topaz/openai/{deploymentName}/response.json and returns it verbatim.
    If no fixture exists, returns a minimal valid ChatCompletion response with a single
    assistant message: "This is a Topaz stub response."
  Fixture management:
  - topaz openai stub set-response --deployment <name> --file <response.json>
    Writes the JSON file to the fixture store.
  - topaz openai stub clear-response --deployment <name>
    Removes the fixture (reverts to default stub response).
  - topaz openai stub list
    Lists all configured deployments and whether a fixture is present.
  Deployment registration (control plane stub, for SDK credential resolution):
  - GET /openai/deployments  — returns the list of all known deployments.
  Authentication: accept any Bearer token (Topaz Entra token or a dummy value) without
  real validation, matching the existing Topaz auth-bypass pattern for emulated services.
  Add GlobalSettings.DefaultOpenAIPort constant and register the service in Topaz.Host.
  Add MCP tool: CreateOpenAIDeployment (name, model) so AI assistants can register
  deployments without CLI.
  milestone: v1.11
  labels: enhancement, openai, ai-agents
-->

### Azure OpenAI Service — Cosmos DB vector search stub

<!--
TODO: Azure Cosmos DB: VectorDistance query support (vector search stub)
  Extend the Cosmos DB SQL query engine (v1.7-beta) to accept queries containing the
  VectorDistance() system function, used by Semantic Kernel's CosmosDBNoSqlVectorStore
  plugin and LangChain's AzureCosmosDBNoSqlVectorSearch:
    SELECT TOP 10 c.id, c.content, VectorDistance(c.embedding, @queryVector) AS score
    FROM c
    ORDER BY VectorDistance(c.embedding, @queryVector)
  Implementation:
  - Detect VectorDistance() in the SELECT and ORDER BY clauses during query parsing.
  - Compute cosine similarity between the stored embedding array and @queryVector parameter.
  - Return results ordered by descending similarity score.
  - If the document has no embedding field, assign score 0.0 and exclude from TOP N results.
  This does not require a real vector index — a linear scan over in-memory documents is
  sufficient for the small datasets used in agent development and integration tests.
  Also handle the flat embedding query pattern used by some SDKs:
    SELECT * FROM c ORDER BY VectorDistance(c.embedding, @v) OFFSET 0 LIMIT 10
  milestone: v1.11
  labels: enhancement, cosmos-db, ai-agents
-->

### ACR Tasks — multi-step task files

<!--
TODO: ACR Tasks: Multi-step task file execution (FileTaskRunRequest)
  Parse the task YAML provided via FileTaskRunRequest (taskFilePath points to a YAML file
  in the cloned context) and execute each step sequentially:
  - build steps: docker build with the specified dockerfile, context, and build-args
  - cmd steps: docker run with the specified image and command
  - push steps: docker push to the local OCI registry
  - when dependencies: topological ordering — a step only starts once all steps listed in
    its `when` array have completed successfully
  Transition the run through Queued → Running → Succeeded / Failed using the same async LRO
  pattern already implemented for DockerBuildRequest (202 Accepted + Azure-AsyncOperation header).
  Stream combined per-step output (prefixed with the step id) to the log content endpoint
  (GET /v2/runs/{runId}/log).
  If any step fails and `continueOnError` is false (the default), mark remaining steps as
  skipped and transition the run to Failed.
  Prerequisite: Docker must be available on the host (falls back to immediate-Succeeded when absent).
  milestone: v1.11
  labels: enhancement, container-registry
-->

<!--
TODO: ACR Tasks: Multi-step task file execution (EncodedTaskRunRequest)
  Extend multi-step execution to the EncodedTaskRunRequest path: base64-decode
  `encodedTaskContent` to recover the YAML, then apply the same step execution logic as
  FileTaskRunRequest above.
  The decoded YAML may also carry an `encodedContext` (base64-encoded tar.gz of the build
  context); if present, extract it to a temporary directory before running any build steps.
  milestone: v1.11
  labels: enhancement, container-registry
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
