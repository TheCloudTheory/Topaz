---
slug: development-updates-11
title: "Topaz Weekly Pulse #11: Azure App Configuration, Application Insights, Private Endpoints, HNS Storage, Kudu Auth, and More"
authors: topaz
tags: [general, app-configuration, application-insights, networking, storage, app-service, service-bus, log-analytics]
---

*This week in Topaz: Azure App Configuration launches as a new first-class service with store and replica lifecycle management, soft-delete, and purge support. Azure Application Insights joins as a new service with CRUD component management, telemetry ingestion, and log query endpoints. Private Endpoint management arrives for virtual networking. Blob and Queue Storage gain Hierarchical Namespace (HNS/ADLS Gen2) and hardened geo-replication secondary-endpoint rejection. App Service grows with publishing credentials and Kudu authentication. Service Bus improves session dead-lettering and dead-letter property propagation. Background service health reporting is enriched with execution timestamps.*

{/* truncate */}


## Case 1: Azure App Configuration — New Service

Azure App Configuration is now a fully supported Topaz service, covering store lifecycle, replica management, and soft-delete semantics.

**Store Management**:
- **Create, Update, Delete, Show, List** — full store lifecycle via `az appconfig` CLI commands and ARM API.
- **SKU Support** — `Free` and `Standard` SKUs are modelled; replica creation is restricted to `Standard` SKU stores, matching Azure semantics.
- **Soft-Delete and Purge** — deleted configuration stores enter a soft-delete state and can be recovered or permanently purged via `az appconfig recover` / `az appconfig purge` and the corresponding ARM endpoints.

**Replica Management**:
- **Create, Delete, Show, List** — replica lifecycle for App Configuration stores is fully implemented, including validation that the parent store uses a supported SKU.
- **E2E Tests** — tests cover store creation, replica provisioning, SKU validation, and purge flows.

**Documentation**:
- A dedicated documentation page covers using the Azure App Configuration SDK against Topaz, including connection string retrieval.

This enables realistic testing of multi-region configuration scenarios and feature-flag workflows without an Azure subscription.


## Case 2: Azure Application Insights — New Service

Azure Application Insights is now a fully supported Topaz service, spanning component management, telemetry ingestion, and log queries.

**Component Management**:
- **Create, Delete, Show, List, Update, Update-Tags** — full component lifecycle via `az monitor app-insights component` CLI commands, the `Az.ApplicationInsights` PowerShell module, and the ARM API.
- **Kind Property** — the `kind` field (`web`, `other`, etc.) is stored and returned, matching the Azure component model.
- **Billing Features** — current billing feature endpoints are implemented, returning default caps that satisfy SDK probing.

**Telemetry Ingestion**:
- **Ingestion Endpoint** — `POST /v2/track` accepts telemetry payloads keyed by instrumentation key, with validation that the key exists; unknown keys return the appropriate error response.
- **E2E Tests** — ingestion tests cover valid and invalid instrumentation key handling.

**Log Queries**:
- **Query Endpoint** — `POST /v1/apps/{appId}/query` accepts Kusto query bodies and returns structured log results, enabling SDK-based query workflows against ingested telemetry.
- **Example Project** — an example project demonstrating telemetry tracking and log querying ships with this release.

**Documentation**:
- README and service documentation updated to reflect full API coverage for Application Insights.
- `Az.ApplicationInsights` PowerShell module is installed in the Dockerfile.

This addition enables end-to-end testing of observability pipelines — from telemetry ingestion to log querying — without an Azure subscription.


## Case 3: Networking — Private Endpoint Management

Private Endpoint CRUD operations are now available, rounding out the virtual networking surface.

**Private Endpoints**:
- **Create, Delete, Get, List** — full private endpoint lifecycle, including IP configuration and availability checks.
- **CLI Commands** — `az network private-endpoint create/delete/show/list` work against Topaz when the `topaz` cloud is active.
- **E2E Tests** — tests verify private endpoint provisioning and retrieval.

Previously unused `System.Net.Http` and `Topaz.Shared` references were cleaned up from networking command files as part of this work.


## Case 4: Storage — Hierarchical Namespace (HNS / ADLS Gen2)

Blob Storage accounts can now be created with Hierarchical Namespace enabled, unlocking ADLS Gen2 scenarios.

**HNS Support**:
- **`--enable-hierarchical-namespace`** — the `CreateStorageAccountCommand` now accepts and persists the HNS flag, matching the Azure `az storage account create --enable-hierarchical-namespace` parameter.
- **E2E Tests** — tests verify that HNS is enabled on the resulting storage account resource.

This fills a gap for IaC templates and data engineering workflows that target ADLS Gen2 without a live Azure subscription.


## Case 5: Storage — Geo-Replication Secondary Endpoint Hardening

Geo-replication fidelity improved with secondary endpoint rejection and queue visibility fixes.

**Secondary Endpoint Rejection**:
- **Queue Service** — `GET /messages` requests targeting the secondary host are now rejected with the correct Azure error response, matching RA-GRS read semantics.
- **Blob and Table** — `originalStorageAccountName` is now threaded through blob and table endpoints to correctly resolve secondary-host requests across all storage services.

**Queue Geo-Replication**:
- Queue visibility tests verify that secondary queues are available before listing, removing a race condition in geo-replication tests.
- `ListPaths` implemented in `ResourceProviderBase`; `ListQueues` consolidated into the queue service control plane.

**Documentation**:
- A geo-replication and disaster-recovery testing scenario is added to the Topaz scenarios documentation.
- Known limitation for secondary endpoint geo-replication lag is documented in the storage account known-limitations page.


## Case 6: App Service — Publishing Credentials and Kudu Authentication

App Service gains publishing credentials management and Kudu endpoint authentication.

**Publishing Credentials**:
- **List Publishing Credentials** — `POST /listPublishingCredentials` returns the first credential for an App Service site, enabling Kudu-based deployment workflows.
- **Validation** — username and password validation with enhanced logging; endpoints and tests updated accordingly.
- **API Coverage** — the List Publishing Credentials operation is documented in the API coverage page.

**Kudu Authentication**:
- **KuduEndpointBase** — Kudu endpoints refactored to inherit from a shared base that implements authorization logic, ensuring consistent auth enforcement across all Kudu routes.

**Resource Identifier Validation**:
- Forbidden character checks added across multiple resource identifier methods, preventing invalid characters in App Service resource names; corresponding tests added.


## Case 7: Service Bus — Session Dead-Lettering and Dead-Letter Property Propagation

Service Bus messaging fidelity improved with session-aware dead-lettering and correct property propagation.

**Session Dead-Lettering**:
- **Session Messages** — dead-lettering logic in `InFlightMessageStore` now correctly handles session messages, ensuring dead-lettered session messages reach the session dead-letter queue.
- **E2E Tests** — tests added for session dead-lettering via `InFlightMessageStore`; DLQ path for session receivers corrected in existing tests.

**Dead-Letter Property Propagation**:
- **ApplicationProperties** — the `OutgoingLinkEndpoint` now extracts the dead-letter reason and description from `ApplicationProperties` (matching the Azure SDK error info format) rather than relying on a single field, improving compatibility with Azure SDK clients.
- **InFlightMessageStore Fix** — dead-letter properties updated to use `ApplicationProperties` for full Azure SDK compatibility.


## Case 8: Log Analytics — Enhanced Data Handling

Log Analytics data ingestion and endpoint handling received further improvements.

- **Ingestion Endpoint Hardening** — data handling in the ingestion endpoint improved; endpoint definitions updated for the new resource endpoint pattern.
- **`ExecutedAt` Property** — background services (including the Log Analytics ingestion processor) now report their last execution timestamp in the `GET /health` response, making it easy to verify that scheduled tasks are running in tests.
- **Resource Endpoint** — `LogAnalyticsResourceProvider` updated to use the standard `ListPaths` infrastructure.


## Case 9: Documentation and LLM Navigation

Documentation received two structural improvements.

**AI / LLM Navigation**:
- **`llms.txt`** — a machine-readable `llms.txt` file is added to the website root, listing key documentation resource links for LLM-based navigation tools.
- **Docusaurus Config** — Docusaurus is updated to surface the `llms.txt` endpoint, improving compatibility with AI coding assistants that probe documentation sites.
- **Agents and LLMs Section** — a new documentation section covers using Topaz with AI agents and LLM-based workflows.

**Java SDK Legacy Tests**:
- Legacy Java tests for the Azure Storage SDK are added to the CI workflow, broadening language coverage for storage compatibility testing.


## Summary: Two New Services and Broad Surface Expansion

This week's changes add two new first-class services (App Configuration and Application Insights) and a new networking primitive (Private Endpoints), while hardening existing services across geo-replication, session dead-lettering, Kudu auth, and HNS storage. The LLM navigation additions signal growing investment in AI-assisted developer workflows against Topaz.

:::tip[Try the New Features]
Everything runs in a single binary — no Azure subscription required.

```bash
docker run -p 8899:8899 thecloudtheory/topaz-host:nightly
```

Create an App Configuration store and replica:

```bash
export SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az group create -n mygroup -l eastus

az appconfig create \
  --name myconfig \
  --resource-group mygroup \
  --location eastus \
  --sku Standard

az appconfig replica create \
  --store-name myconfig \
  --resource-group mygroup \
  --name myreplica \
  --location westus
```

Ingest and query Application Insights telemetry:

```bash
az monitor app-insights component create \
  --app myinsights \
  --resource-group mygroup \
  --location eastus \
  --kind web

# Use the Azure Monitor SDK to POST telemetry to /v2/track
# and query logs via /v1/apps/{appId}/query
```
:::
