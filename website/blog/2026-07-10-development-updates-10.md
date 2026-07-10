---
slug: development-updates-10
title: "Topaz Weekly Pulse #10: Azure Log Analytics, Cosmos DB Entra ID Auth, Geo-Replication, Management Group Expansion, and More"
authors: topaz
tags: [general, log-analytics, cosmos-db, service-bus, storage, arm, compute, vscode]
---

*This week in Topaz: Azure Log Analytics launches as a new first-class service with workspace management, data ingestion, and soft-delete support. Cosmos DB gains Entra ID data-plane authorization with resource-scoped RBAC, GROUP BY/ORDER BY query support, and an expired document purge scheduler. Blob Storage adds geo-replication sync simulation. ARM expands with management group child retrieval, tenant-level resource provider listing, and tenant function support in deployment parameters. Service Bus gains a message expiry scheduler and dead-letter forwarding. The VS Code extension is now documented.*

{/* truncate */}


## Case 1: Azure Log Analytics — New Service

Azure Log Analytics is now a fully supported Topaz service, covering both workspace lifecycle management and data ingestion.

**Workspace Management**:
- **Create, Update, Delete, List, Show** — full workspace lifecycle via `az monitor log-analytics workspace` CLI commands and ARM API.
- **Soft-Delete and Purge Protection** — soft-delete and purge protection endpoints implemented, matching the Azure workspace deletion semantics (workspaces can be recovered within the retention window).
- **CLI Commands** — `az monitor log-analytics workspace create/delete/show/list/update` commands work against Topaz automatically when the `topaz` cloud is active.

**Data Ingestion**:
- **Ingestion Endpoint** — `POST /dataCollectionRules/{dcr}/streams/{stream}` accepts log payloads and stores them, enabling SDK-based log ingestion workflows.
- **E2E Tests** — end-to-end tests verify workspace provisioning and log ingestion using the Azure Monitor Ingestion SDK.

**Service Status**:
- Log Analytics is added to the service status table and README with full API coverage noted.

This addition enables testing of observability pipelines that depend on custom log ingestion without an Azure subscription.


## Case 2: Cosmos DB — Entra ID Authorization and Query Improvements

Cosmos DB received significant authorization improvements, bringing it closer to production Azure behavior with Entra ID support and fine-grained RBAC.

**Entra ID Data-Plane Authorization**:
- **DataPlaneAuthorizationChecker** — new component that evaluates role assignments and permissions for data-plane requests, enforcing resource-scoped RBAC.
- **Resource-Scoped RBAC** — role assignments are now enforced at the resource level (collection, database, account), matching Azure Cosmos DB RBAC semantics.
- **DisableLocalAuth** — `disableLocalAuth` account property is now respected: when set, key-based authentication is rejected in favour of Entra ID tokens.
- **readMetadata Permission** — `readMetadata` permission coverage added to RBAC tests, verifying metadata access control.

**SQL Query Engine**:
- **GROUP BY** — `GROUP BY` clause is now supported in Cosmos DB SQL queries, with aggregation functions applied per group.
- **ORDER BY** — `ORDER BY` support completed; the previous TODO is removed.
- **IN Expression** — evaluation logic for `IN` expressions simplified and made more robust.
- **Alias Handling** — `CosmosDbSqlParser` updated to support keywords as aliases in `SELECT` projections.

**Document Lifecycle**:
- **ExpiredDocumentsPurgeScheduler** — background scheduler detects and removes TTL-expired documents, matching Azure's automatic document expiry behaviour.
- **ICosmosDbDataPlane / ICosmosDbControlPlane Interfaces** — new interfaces introduced, enabling cleaner separation of concerns and easier unit testing.

These changes allow realistic testing of applications that rely on Entra ID for Cosmos DB access control or depend on TTL-based document expiry.


## Case 3: Blob Storage — Geo-Replication Sync Simulation

Azure Blob Storage now simulates geo-replication, enabling tests of applications that read from secondary regions or inspect replication lag.

**Geo-Replication**:
- **GeoReplicationSyncScheduler** — background scheduler updates `LastGeoSyncTime` on a configurable interval, simulating the replication lag present in Azure RA-GRS accounts.
- **Service Stats** — `GET /?restype=service&comp=stats` returns realistic geo-replication status including last sync time, matching the Azure Storage service stats contract.
- **Known Limitation Tracked** — geo-replication lag simulation on secondary reads is tracked in the backlog for future implementation.

**E2E Tests**:
- Tests verify `LastGeoSyncTime` calculation and service stats response format, confirming compatibility with Azure Storage SDK clients.


## Case 4: ARM — Management Groups, Tenant Providers, and Deployment Improvements

ARM received several expansions this week covering management group traversal, tenant-level provider listing, and deployment parameter improvements.

**Management Groups**:
- **Child Retrieval** — `GET /managementGroups/{id}?$expand=children` now returns child management groups and subscriptions, enabling realistic management group tree traversal in IaC tests.
- **E2E Tests** — tests verify correct child expansion behaviour.

**Resource Providers**:
- **ListResourceProvidersByTenant** — `GET /providers` at tenant scope implemented, returning the full list of registered resource providers; validated with an E2E test.
- **ListSubscriptionResourcesEndpoint** — enhanced to support resource groups and additional resource types when listing subscription-level resources.

**Deployments**:
- **Tenant Function as Parameter Default** — ARM template deployments now resolve `tenant()` function calls used as parameter default values, removing a compatibility gap with bicep-generated templates.
- **TenantMetadata** — `TenantMetadata` class introduced to carry tenant information through deployment evaluation, supporting the tenant function.

**Resource Groups**:
- Resource type strings normalised for consistency across `ResourceGroupResource` and `ResourceManagerControlPlane`.
- Resource ID generation refactored in `ListResourceGroupResourcesEndpoint` for correctness.


## Case 5: Service Bus — Message Expiry and Dead-Letter Forwarding

Service Bus messaging fidelity improved with time-to-live expiry and configurable dead-letter forwarding.

**Message Expiry**:
- **Message Expiry Scheduler** — background scheduler monitors messages and moves expired messages (those exceeding their TTL) to the dead-letter queue, matching Azure Service Bus TTL semantics.
- This enables tests that verify expiry-triggered dead-lettering without waiting on real clock time — Topaz's time injection can accelerate TTL expiry.

**Dead-Letter Forwarding**:
- **ForwardDeadLetteredMessagesTo** — messages dead-lettered from a queue or subscription are now forwarded to the configured `ForwardDeadLetteredMessagesTo` target, matching the Azure Service Bus queue/subscription property.
- Documentation updated to reflect full dead-letter queue support.


## Case 6: Compute — VM SKU Catalogue Expansion

The virtual machine SKU catalogue is expanded with Premium IO–capable SKUs.

- **PremiumIO Support** — VM SKUs now include the `PremiumIO` capability flag where applicable, matching the Azure `GET /skus` response for Premium SSD–eligible VM sizes.
- **SKU Listing Test** — test added to verify SKU listing returns expected capability metadata.

This fills a gap for IaC templates that query SKU capabilities before provisioning Premium SSD disks.


## Case 7: VS Code Extension — Experimental Feature Documentation

The experimental Topaz VS Code extension is now documented in the official docs and website.

- **Features** — the extension surfaces Topaz service status, resource browsing, and deployment management directly in the VS Code sidebar.
- **Documentation** — a dedicated docs page and website section describe extension features and installation, including a feature screenshot.
- **ServiceCard** — extension icon added to the website's service card component.

The extension remains experimental; production readiness is tracked separately.


## Case 8: Infrastructure — Docker Certificate and Chaos Health

Miscellaneous infrastructure and observability improvements ship this week.

**Docker**:
- Dockerfile updated to install the Topaz TLS certificate at build time, removing manual certificate trust steps for containerised deployments.
- `.dockerignore` updated to allow certificate files into the image.
- Docker run command corrected to use the right host port.
- Documentation note added for `.NET` certificate trust on macOS.

**Chaos / Health**:
- `GET /health` response now includes the current chaos mode state, making it easy to assert whether fault injection is active in a test setup.
- E2E tests verify chaos state is correctly reflected in the health endpoint response.


## Summary: Observability, Authorization, and Protocol Depth

This week's changes broaden Topaz's service coverage (Log Analytics) while deepening two critical areas: authorization fidelity (Cosmos DB Entra ID RBAC, DisableLocalAuth) and protocol completeness (Service Bus TTL expiry, Blob geo-replication, ARM tenant functions). The VS Code extension reaching documentation milestone signals it is approaching general availability.

:::tip[Try the New Features]
Everything runs in a single binary — no Azure subscription required.

```bash
docker run -p 8899:8899 thecloudtheory/topaz-host:nightly
```

Ingest logs into Azure Log Analytics with Topaz:

```bash
export SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az group create -n mygroup -l eastus

az monitor log-analytics workspace create \
  --resource-group mygroup \
  --workspace-name myworkspace \
  --location eastus

# Use the Azure Monitor Ingestion SDK to POST logs to the ingestion endpoint
```

Test Cosmos DB with Entra ID authorization:

```bash
az cosmosdb create \
  --name myaccount \
  --resource-group mygroup \
  --disable-local-auth true

# Assign a Cosmos DB data plane role to your identity, then use
# DefaultAzureCredential — key-based auth will be rejected
```
:::
