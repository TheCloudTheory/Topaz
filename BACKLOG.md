# Backlog

Central place for planning upcoming work. Each TODO block below is automatically
converted to a GitHub Issue by CI when new lines are committed.

> **Note:** The action only picks up TODOs that appear in the *diff* of a new commit.
> To bulk-import existing items, run the _TODO to Issue_ workflow manually from the
> **Actions** tab.

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

## v1.10-preview

<!--
TODO: Storage Account — geo-replication lag on secondary reads
  Secondary reads currently return the same data as the primary (perfectly in sync).
  Simulate replication lag so secondary reads reflect the state at the last GeoReplicationSyncScheduler
  tick rather than the live primary state:
  - Journal writes to the primary data store with a timestamp (enqueue-time).
  - Secondary read endpoints (blob container listing, queue listing, table entity reads on
    {accountName}-secondary.* hosts) filter out writes whose enqueue-time is later than the
    account's persisted LastGeoSyncTime, returning a snapshot of the data as it existed at
    the last scheduler tick.
  - After each GeoReplicationSyncScheduler tick (every 30 s), the visible secondary snapshot
    advances to include all writes up to the new LastGeoSyncTime.
  - Non-RA-GRS/RAGZRS accounts are unaffected.
  Prerequisites: GeoReplicationSyncScheduler (v1.9-preview, done).
  See also: website/docs/known-limitations.md — "Storage Account — secondary endpoint reads return primary data".
  milestone: v1.10-preview
  labels: enhancement, storage
-->

<!--
TODO: App Configuration: Replicas companion endpoint
  The azurerm Terraform provider calls GET .../configurationStores/{name}/replicas after
  creation. Add a stub endpoint that returns {"value":[]} (Topaz does not emulate replicas).
  Endpoint:
  - GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.AppConfiguration/configurationStores/{name}/replicas
  milestone: v1.10-preview
  labels: enhancement, app-configuration
-->

<!--
TODO: App Configuration: Soft-delete companion endpoints
  The azurerm Terraform provider calls soft-delete endpoints during destroy. Add stubs that
  return 404 (Topaz does not emulate soft-delete / purge protection):
  - GET /subscriptions/{sub}/providers/Microsoft.AppConfiguration/locations/{location}/deletedConfigurationStores/{name}
  - GET /subscriptions/{sub}/providers/Microsoft.AppConfiguration/locations/{location}/deletedConfigurationStores
  - POST .../deletedConfigurationStores/{name}/purge
  milestone: v1.10-preview
  labels: enhancement, app-configuration
-->

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

### Resource pre-seeding from existing Azure subscriptions

<!--
TODO: Resource pre-seeding — import existing Azure resources into Topaz state
  Allow users to seed the Topaz local state from a live Azure subscription so that
  deployments can be tested against resources that were manually created or provisioned
  by a different template — without requiring a lower environment.

  Proposed flow:
  - `topaz seed --subscription <sub> [--resource-group <rg>] [--resource-type <type>]`
    Calls the real Azure ARM API (via DefaultAzureCredential) to list resources in the
    target scope and writes a metadata.json for each supported resource type into the
    local .topaz/ directory tree, using the same filesystem layout as Topaz's own
    CreateOrUpdate path.
  - Only resource types that Topaz implements are seeded; unsupported types are listed
    as warnings so the user knows what coverage gaps remain.
  - Dry-run flag (`--dry-run`) prints the resources that would be seeded without
    writing any files.
  - Overwrite flag (`--overwrite`) controls whether existing local state is replaced.

  This closes the primary gap identified by users who deploy templates against
  pre-existing infrastructure: reference() expressions and incremental deployments
  can be validated locally once the existing resource state is present in Topaz.
  milestone: v1.11
  labels: enhancement, good first issue
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

## v1.11

### Azure Event Grid — initial control plane and delivery

<!--
TODO: Azure Event Grid: New service scaffold
  Add Topaz.Service.EventGrid project with EventGridTopicResource, EventGridTopicResourceProperties,
  resource provider, control plane (including Deploy()), host registration, and RouteDeployment()
  case for Microsoft.EventGrid/topics.
  milestone: v1.11
  labels: enhancement, event-grid, good first issue
-->

<!--
TODO: Azure Event Grid: Topic CRUD
  Create, get, update (tags, inputSchema, publicNetworkAccess), delete, list by resource group,
  and list by subscription; endpoint emitted as https://{name}.eventgrid.topaz.local.dev:<port>/;
  access keys generated on creation and exposed via listKeys / regenerateKey.
  milestone: v1.11
  labels: enhancement, event-grid
-->

<!--
TODO: Azure Event Grid: Event Subscriptions CRUD
  Create, get, update, delete, and list event subscriptions per topic; fields: destination
  (WebHook, ServiceBus, EventHub), filter, eventDeliverySchema; persisted as subresources.
  milestone: v1.11
  labels: enhancement, event-grid
-->

<!--
TODO: Azure Event Grid: Event publishing
  POST /api/events on the topic data-plane endpoint accepts CloudEvents and EventGrid schema arrays;
  persists events and delivers synchronously to WebHook destinations via HTTP POST.
  milestone: v1.11
  labels: enhancement, event-grid
-->

<!--
TODO: Azure Event Grid: System Topics CRUD
  Create, get, delete, and list Microsoft.EventGrid/systemTopics; source and topicType stored and
  returned verbatim; event subscriptions on system topics follow the same model as custom topics.
  milestone: v1.11
  labels: enhancement, event-grid
-->

<!--
TODO: Azure Event Grid: MCP provisioning tool
  Add CreateEventGridTopic MCP tool; extend GetConnectionStrings with Event Grid endpoint and key.
  milestone: v1.11
  labels: enhancement, event-grid, mcp
-->

### App Configuration — advanced data plane features

<!--
TODO: App Configuration: Snapshots API
  Implement PUT/GET/DELETE /snapshots/{name} — capture a point-in-time copy of key-values matching
  a filter; archiveSnapshot and recoverSnapshot transitions; snapshot status (provisioning → ready /
  archived); list snapshots with ?name=, ?status= filtering.
  milestone: v1.11
  labels: enhancement, app-configuration
-->

<!--
TODO: App Configuration: Key Vault references
  Key-values with content type application/vnd.microsoft.appconfig.keyvaultref+json resolved on
  GET /kv/{key}?resolve=true via the local Topaz Key Vault instance; compatible with
  AzureAppConfigurationOptions.ConfigureKeyVault() in the .NET SDK.
  milestone: v1.11
  labels: enhancement, app-configuration, key-vault
-->

<!--
TODO: App Configuration: Change notification via EventGrid integration
  On any key-value write or delete, publish a Microsoft.AppConfiguration.KeyValueModified /
  Microsoft.AppConfiguration.KeyValueDeleted event to any EventGrid topic subscription wired to
  the store's system topic.
  Prerequisite: Event Grid system topics and event publishing (above).
  milestone: v1.11
  labels: enhancement, app-configuration, event-grid
-->

### Application Insights & Log Analytics — richer KQL

<!--
TODO: Application Insights / Log Analytics: Extended KQL operators
  Add join (inner/leftouter), mv-expand, bin(), ago(), and time-range filter (between, datetime())
  to both the Application Insights query API and Log Analytics query API introduced in v1.9.
  milestone: v1.11
  labels: enhancement, application-insights, log-analytics
-->

<!--
TODO: Log Analytics: Cross-workspace query
  POST /v1/workspaces/{id}/query accepts a workspaces() expression referencing other emulated
  Log Analytics workspaces within the same Topaz instance.
  milestone: v1.11
  labels: enhancement, log-analytics
-->

### Azure Redis Cache — data plane

<!--
TODO: Azure Redis Cache: RESP2 protocol listener
  In-process TCP listener on port 6379 (configurable) implementing the RESP2 wire protocol;
  backed by a ConcurrentDictionary per-cache instance; supports SET, GET, DEL, EXISTS, EXPIRE,
  TTL, KEYS, HSET, HGET, HGETALL, HDEL, LPUSH, RPUSH, LPOP, RPOP, LRANGE, SADD, SMEMBERS,
  SREM, INCR, DECR, PING, SELECT, FLUSHDB.
  milestone: v1.11
  labels: enhancement, redis
-->

<!--
TODO: Azure Redis Cache: TLS listener
  Optional TLS-wrapped RESP2 listener on port 6380 using the existing Topaz dev certificate;
  enabled when enableNonSslPort is false on the cache resource.
  milestone: v1.11
  labels: enhancement, redis
-->

<!--
TODO: Azure Redis Cache: Connection string in GetConnectionStrings
  MCP and CLI GetConnectionStrings emit {host}:{port},password={key},ssl={true|false},abortConnect=False
  format compatible with StackExchange.Redis.ConfigurationOptions.Parse().
  milestone: v1.11
  labels: enhancement, redis, mcp
-->

---

## v1.12

### API Management — policy execution subset

<!--
TODO: API Management: rate-limit policy
  Enforce call-rate and bandwidth quotas per subscription key; 429 response with Retry-After header
  when the limit is exceeded; in-memory per-process counter.
  milestone: v1.12
  labels: enhancement, api-management
-->

<!--
TODO: API Management: set-header / set-body / rewrite-uri policies
  Transform inbound and outbound headers, replace the request body, and rewrite the upstream URL
  before forwarding to the backend.
  milestone: v1.12
  labels: enhancement, api-management
-->

<!--
TODO: API Management: validate-jwt policy
  Validate Authorization: Bearer tokens against a configurable JWKS URI or inline signing key;
  reject with 401 on invalid or expired tokens.
  milestone: v1.12
  labels: enhancement, api-management
-->

<!--
TODO: API Management: Backend request forwarding
  When a matching API + operation is found and a backend is configured, forward the request to
  backend.url and stream the response back; honours rewrite-uri and set-header transforms.
  milestone: v1.12
  labels: enhancement, api-management
-->

### Service Bus — dead-letter queue and scheduled messages

<!--
TODO: Service Bus: Dead-letter queue (DLQ)
  AMQP and HTTP sub-queue path /$DeadLetterQueue per queue and per subscription;
  DeadLetter() receiver API moves messages with DeadLetterReason and DeadLetterErrorDescription
  annotations; maxDeliveryCount enforcement auto-dead-letters after threshold.
  milestone: v1.12
  labels: enhancement, service-bus, amqp
-->

<!--
TODO: Service Bus: Scheduled message delivery
  ScheduledEnqueueTimeUtc broker property respected on send; background scheduler enqueues messages
  at the specified time; CancelScheduledMessage cancels by sequence number before delivery.
  milestone: v1.12
  labels: enhancement, service-bus
-->

<!--
TODO: Service Bus: Message deferral
  Defer() receiver API parks a message by sequence number; ReceiveDeferred(sequenceNumber) retrieves
  it explicitly; deferred messages invisible to normal Receive().
  milestone: v1.12
  labels: enhancement, service-bus, amqp
-->

### Event Hub — consumer groups and offset checkpointing

<!--
TODO: Event Hub: Consumer group epoch tracking
  Each consumer group tracks the AMQP receiver epoch; attaches with a higher epoch steal the link
  from a lower-epoch receiver; ReceiverDisconnectedException raised on the evicted client.
  milestone: v1.12
  labels: enhancement, event-hub, amqp
-->

<!--
TODO: Event Hub: Checkpoint Store simulation
  In-process checkpoint store compatible with BlobCheckpointStore; EventProcessorClient can read
  and write partition ownership and offsets without a real Storage account; backed by the emulated
  Blob Storage service when a storage account is configured.
  milestone: v1.12
  labels: enhancement, event-hub
-->

<!--
TODO: Event Hub: Partition cursor persistence
  Sequence number and offset per partition per consumer group persisted across restarts;
  EventPosition.FromSequenceNumber, FromOffset, FromEnqueuedTime, and Earliest/Latest all respected.
  milestone: v1.12
  labels: enhancement, event-hub
-->

---

## v1.13

### Azure Container Instances — real Docker execution

<!--
TODO: Azure Container Instances: Docker-backed container group execution
  When the local Docker daemon is available, Create container group spawns real containers via the
  Docker API; instanceView.state reflects actual container status (Pending → Running → Terminated);
  opt-in via TOPAZ_ACI_USE_DOCKER=true environment variable.
  milestone: v1.13
  labels: enhancement, container-instances
-->

<!--
TODO: Azure Container Instances: Container log streaming
  GET .../containers/{name}/logs streams stdout/stderr from the running Docker container;
  ?tail=N and ?timestamps=true query parameters supported.
  milestone: v1.13
  labels: enhancement, container-instances
-->

<!--
TODO: Azure Container Instances: Local Container Registry integration
  Container image names resolved against the emulated Container Registry (port 8892) before
  pulling from the public registry; enables end-to-end az acr build → az container create workflows.
  milestone: v1.13
  labels: enhancement, container-instances, container-registry
-->

### Azure Functions — deeper App Service integration

<!--
TODO: Azure Functions: Function App resource type
  Microsoft.Web/sites with kind: functionapp — distinct provisioning path that emits a
  function-specific hostNames pattern and validates required app settings
  (AzureWebJobsStorage, FUNCTIONS_EXTENSION_VERSION).
  milestone: v1.13
  labels: enhancement, app-service, functions
-->

<!--
TODO: Azure Functions: Admin management API
  GET /admin/functions lists deployed functions with trigger type and invoke URL;
  POST /admin/functions/{name} manually invokes a function (requires the app to be forwarded
  via the App Service proxy).
  milestone: v1.13
  labels: enhancement, functions
-->

<!--
TODO: Azure Functions: host.json parsing and validation
  Read FUNCTIONS_EXTENSION_VERSION and AzureWebJobsStorage from app settings; validate that the
  referenced Storage account exists in the emulator and surface a warning in the Topaz portal if not.
  milestone: v1.13
  labels: enhancement, functions
-->

### Chaos Engineering — AMQP-level fault injection

<!--
TODO: Chaos Engineering: AMQP link detach injection
  Chaos rules with faultType: AmqpLinkDetach forcibly close AMQP sender/receiver links at the
  configured faultRate; clients receive an AMQP detach frame with
  error.condition = amqp:resource-limit-exceeded.
  milestone: v1.13
  labels: enhancement, chaos, service-bus, event-hub, amqp
-->

<!--
TODO: Chaos Engineering: Credit starvation
  faultType: AmqpCreditStarvation suspends flow-frame credit grants on a link, causing the
  client's send buffer to fill; tests client-side timeout and retry logic.
  milestone: v1.13
  labels: enhancement, chaos, service-bus, event-hub, amqp
-->

<!--
TODO: Chaos Engineering: Session timeout injection
  faultType: AmqpSessionTimeout closes the AMQP session (not just the link) after a configurable
  delay, simulating broker-side session expiry.
  milestone: v1.13
  labels: enhancement, chaos, service-bus, event-hub, amqp
-->

---

## v1.14

### OpenTofu — verified compatibility

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
  milestone: v1.14
  labels: enhancement, good first issue
-->

### Azure Service Health / Resource Health stubs

<!--
TODO: Azure Service Health: Resource Health availability statuses
  GET /subscriptions/{sub}/providers/Microsoft.ResourceHealth/availabilityStatuses and per-resource
  GET .../providers/Microsoft.ResourceHealth/availabilityStatuses/current always return Available;
  unblocks SDK health-check decorators and ARM health-check extensions in production apps.
  milestone: v1.14
  labels: enhancement, resource-health
-->

<!--
TODO: Azure Service Health: Service Health events
  GET /subscriptions/{sub}/providers/Microsoft.ResourceHealth/events returns an empty list by
  default; controllable via chaos mode to inject a synthetic ServiceIssue or PlannedMaintenance event.
  milestone: v1.14
  labels: enhancement, resource-health, chaos
-->

### IaC state export

<!--
TODO: IaC state export: topaz export --format bicep
  Introspects the current emulator state and generates a Bicep file describing all provisioned
  resources across a subscription; respects resource dependencies for module ordering.
  milestone: v1.14
  labels: enhancement, cli
-->

<!--
TODO: IaC state export: topaz export --format terraform
  Same introspection emitting HCL with azurerm provider resource blocks; includes a
  terraform.tfvars stub for subscription and tenant IDs.
  milestone: v1.14
  labels: enhancement, cli, terraform
-->

<!--
TODO: Service Bus: Chained DLQ forwarding (A → B DLQ → C)
  When ForwardDeadLetteredMessagesTo is set on the target entity B, messages forwarded
  from A's DLQ to B that subsequently land in B's DLQ should themselves be forwarded
  to C (B's ForwardDeadLetteredMessagesTo target), and so on recursively.
  The current v1.9-preview implementation (single-hop forwarding) does not follow the
  chain: a message forwarded to B and then dead-lettered in B stays in B/$deadletterqueue.
  Prerequisite: ForwardDeadLetteredMessagesTo single-hop support (v1.9-preview).
  Required changes:
  - In InFlightMessageStore.DeadLetterCore, after resolving the first forwarding target,
    loop (up to a bounded depth, e.g. 5 hops) to follow subsequent
    ForwardDeadLetteredMessagesTo links on each intermediate entity.
  - Break the loop when no further ForwardDeadLetteredMessagesTo is set, the target
    does not exist, or the hop limit is reached; enqueue to the final entity's main queue
    or to its local DLQ if the entity does not forward.
  milestone: v1.14
  labels: enhancement, service-bus
-->

## v1.15

### Log Analytics — soft-delete and purge protection

<!--
TODO: Log Analytics: Soft-delete companion endpoints
  The azurerm Terraform provider calls soft-delete endpoints during destroy. Add stubs that
  return 404 (Topaz does not emulate soft-delete / purge protection):
  - GET /subscriptions/{sub}/providers/Microsoft.OperationalInsights/deletedWorkspaces/{name}
  - GET /subscriptions/{sub}/providers/Microsoft.OperationalInsights/locations/{location}/deletedWorkspaces
  - POST /subscriptions/{sub}/providers/Microsoft.OperationalInsights/locations/{location}/deletedWorkspaces/{name}/purge
  The GET /subscriptions/{sub}/providers/Microsoft.OperationalInsights/deletedWorkspaces
  endpoint already exists as a stub (required for workspace creation). This work extends it
  with the per-workspace and location-scoped variants.
  milestone: v1.15
  labels: enhancement, log-analytics
-->

<!--
TODO: Log Analytics: Workspace soft-delete and restore
  Implement soft-delete semantics on workspace deletion so that az monitor log-analytics workspace
  delete (without --force) moves the workspace to a deleted state rather than immediately purging it:
  - DELETE without query param ?force=true soft-deletes the workspace (sets deletedDate, retains data
    for softDeleteRetentionInDays, returns 200 with ProvisioningState = Deleting).
  - GET on a soft-deleted workspace returns the workspace with ProvisioningState = Deleting and
    a deletedDate timestamp.
  - GET /subscriptions/{sub}/providers/Microsoft.OperationalInsights/deletedWorkspaces returns
    soft-deleted workspaces as a list.
  - POST .../deletedWorkspaces/{name}/purge immediately removes the workspace and its data.
  - DELETE with ?force=true bypasses soft-delete and purges immediately.
  - Restoring a soft-deleted workspace is achieved by calling CreateOrUpdate with the same name;
    ProvisioningState transitions back to Succeeded.
  Prerequisite: soft-delete companion endpoints stub (above).
  milestone: v1.15
  labels: enhancement, log-analytics
-->
