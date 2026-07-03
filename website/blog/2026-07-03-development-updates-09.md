---
slug: development-updates-09
title: "Topaz Weekly Pulse #9: Azure App Configuration, Service Bus Sessions, Blob Auth Enforcement, ARM Deployment Operations, and More"
authors: topaz
tags: [general, app-configuration, service-bus, blob-storage, arm, container-registry, cli]
---

*This week in Topaz: Azure App Configuration is fully implemented with data plane key-value management, access key operations, and Azure CLI support. Service Bus gains session-based messaging with session lock management, wildcard session attachment, and dead-letter queue semantics. Blob Storage now enforces authentication for private containers. ARM deployment operations are now tracked across all four scopes. Container Registry receives async Docker build execution. The Topaz CLI gains defaults and health management commands.*

{/* truncate */}


## Case 1: Azure App Configuration — New Service

Azure App Configuration is now a first-class Topaz service, covering both control plane provisioning and data plane key-value operations.

**Control Plane**:
- **Create, Delete, Show, List** — full lifecycle management of App Configuration stores via `az appconfig` CLI commands and ARM API.
- **Access Key Management** — `ListKeys` and `RegenerateKey` operations are fully supported, returning primary/secondary key pairs with connection strings.
- **Companion Endpoints** — `/replicas` and soft-delete (`/deletedConfigurationStores`) endpoints are implemented, matching the full Azure Resource Manager contract.
- **Compatibility Matrix** — App Configuration is added to Topaz's compatibility matrix, covering ARM, Azure CLI, and SDK scenarios.

**Data Plane**:
- **Key-Value CRUD** — create, read, update, and delete key-value pairs via the App Configuration REST API.
- **ListKvs** — list key-values with detailed logging for diagnostics; supports filtering by key prefix and label.
- **Azure SDK Compatibility** — E2E tests verify that `ConfigurationClient` from the Azure SDK works against Topaz without modification.

**CLI**:
- Full set of `az appconfig kv` commands: `set`, `show`, `delete`, `list`.
- Commands use the Topaz endpoint automatically when the `topaz` cloud is active.

This addition means Topaz now covers feature flag and configuration management scenarios end-to-end, without any external dependency.


## Case 2: Service Bus — Sessions, Dead-Letter, and AMQP Improvements

Service Bus received a major round of AMQP-level improvements, adding session semantics, dead-letter handling, and more robust receiver validation.

**Session-Based Messaging**:
- **Session Receiver Support** — `LinkProcessor` now correctly handles session-based receivers, enforcing that session-enabled entities require session receivers and rejecting non-session receivers.
- **Session Lock Management** — session locks are tracked and validated in `SessionMessageStore`, preventing concurrent access to the same session from multiple receivers.
- **Wildcard Session Attachment** — receivers using wildcard session attachment (`*`) correctly remain pending when no sessions are available, matching Azure Service Bus behavior.
- **Session Token in Document Endpoints** — session token format updated for compatibility with Azure SDK session handling.

**Dead-Letter Queue**:
- **Dead-Letter Semantics** — messages that exceed delivery count or are explicitly dead-lettered are moved to the dead-letter sub-queue (`$DeadLetterQueue`).
- **E2E Tests** — end-to-end tests verify complete dead-letter workflows using the Azure Service Bus SDK.
- **Lock Token Extraction** — improved lock token extraction logic for reliable dead-letter operations.

**Session Management Fixes**:
- Wildcard session `ATTACH` is correctly left pending (not immediately rejected) when no sessions are available.
- Queue creation callback clears stale messages in `SessionMessageStore` to prevent ghost message delivery.
- `renew session` lock expiration format corrected for compatibility.

These improvements make Service Bus sessions production-realistic, enabling test scenarios that depend on competing consumers, session isolation, and dead-letter processing.


## Case 3: Blob Storage — Authentication Enforcement for Private Containers

Private Azure Blob Storage containers now correctly enforce authentication, aligning Topaz's behavior with Azurite and production Azure.

**Authentication Enforcement**:
- Requests to private containers without valid credentials are now rejected with `401 Unauthorized`.
- Public containers continue to allow anonymous access, matching the Azure access tier semantics.
- HMAC-SHA256 signature verification added to Account SAS validation, with encryption scope handling.

**Azurite Comparison**:
- Topaz now matches Azurite's authentication behavior for private containers, confirmed by updated comparison documentation.
- Security validation annotations (CodeQL comments) added to `AccountSasValidator` to mark reviewed security-sensitive code paths.

This closes a gap where unauthenticated requests to private containers would succeed against Topaz but fail against Azure.


## Case 4: ARM — Deployment Operations Tracking Across All Scopes

Deployment operations are now tracked at all four ARM deployment scopes, enabling realistic Infrastructure-as-Code testing with full operation history.

**Deployment Operations**:
- **Resource Group Scope** — `GET /resourceGroups/{rg}/deployments/{name}/operations` and `GET .../operations/{operationId}` return per-operation status and provisioning details.
- **Subscription Scope** — deployment operations tracked and returned for subscription-level deployments, including `GetDeploymentById` endpoint.
- **Management Group Scope** — deployment operations and validation endpoints for management group scope deployments.
- **Tenant Scope** — tenant-scope deployment operations list and cleanup implemented.

**Deployment Improvements**:
- **Symbolic Resource Name Mapping** — ARM template output evaluation now resolves symbolic resource names in nested deployment references.
- **Role Definitions** — role definitions endpoint enhanced with improved reference expression resolution for nested deployments.
- **Long-Running Disk Operations** — disk access creation returns a proper long-running operation with polling support.

**Test Coverage**:
- Tests for listing deployments at tenant scope and cleaning up tenant-scope deployment operations.
- Subscription scope deployment template test added.
- Legacy path support and `operationId` extraction updated for broader ARM client compatibility.


## Case 5: Container Registry — Docker Build Execution

Azure Container Registry now supports ACR Tasks–style Docker build request execution, enabling build-and-push workflows in tests.

**DockerBuildRequest**:
- `POST /registries/{registry}/scheduleRun` with `DockerBuildRequest` payload triggers an async Docker build.
- Build logs are streamed and retrievable via the run log endpoint, including range-based log retrieval via `Range` header.
- Async operation status endpoint tracks build run status through to completion.

**Registry Management**:
- List, create, and delete operations for container registries implemented.
- CLI support for registry CRUD operations added.
- Known limitation: multi-step ACR task file execution is not yet supported (tracked for v1.11).


## Case 6: Public IP Addresses — New Resource

Azure Public IP Addresses are now manageable in Topaz.

- **CRUD** — create, read, update, delete Public IP Address resources.
- **CLI commands** — `az network public-ip` commands work against Topaz.
- **E2E tests** — tests verify provisioning and property retrieval via ARM API.


## Case 7: Topaz CLI — Defaults and Health Commands

The Topaz CLI gains quality-of-life features for local development workflows.

**Defaults Management**:
- `topaz defaults set` and `topaz defaults show` — persist commonly-used values (subscription ID, resource group, location) so commands can be run without repeating flags.
- Configurable defaults are now fully implemented, removing the last placeholder TODOs.
- `SubscriptionId` is now nullable across commands, allowing commands that don't require a subscription to run without one.

**Health and Configuration**:
- `topaz health` — reports service health status for all emulated services.
- `topaz config` — shows current Topaz configuration and active defaults.
- Chaos mode status is now included in the `StatusTool` output, making it easy to verify whether fault injection is active.

**Service Display**:
- Service status table refactored to clearly separate Azure-emulated services from Topaz-internal services, improving readability of the startup output.


## Case 8: Chaos Engineering — Namespace-Targeted Fault Injection

Chaos engineering and fault injection capabilities are improved for precision testing.

- **Namespace Targeting** — chaos injection can now target specific service namespaces (e.g., only inject faults into Service Bus, not Storage), avoiding unintended side effects on other services during tests.
- **Retry Logic Testing** — documentation updated with examples of testing Azure SDK retry logic in .NET, Python, and JavaScript using Topaz fault injection.
- **Comparison Documentation** — chaos engineering capabilities added to Topaz vs. Azurite and Topaz vs. Azure comparison docs.


## Summary: Broader Service Coverage, Deeper AMQP Fidelity

This week's changes advance Topaz on two fronts: new service coverage (App Configuration, Public IP, full deployment operation tracking) and deeper protocol fidelity (Service Bus sessions, Blob auth enforcement, SAS HMAC verification). The CLI improvements round out the local developer experience, making it easier to manage test environments without reaching for the Azure portal.

:::tip[Try the New Features]
Everything runs in a single binary — no Azure subscription required.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

Use Azure App Configuration with Topaz:

```bash
export SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az group create -n mygroup -l eastus

az appconfig create \
  --name myconfig \
  --resource-group mygroup \
  --location eastus

az appconfig kv set \
  --name myconfig \
  --key "Settings:Theme" \
  --value "dark" \
  --yes

az appconfig kv list --name myconfig
```

Test Service Bus session receivers:

```bash
az servicebus namespace create \
  --name mynamespace \
  --resource-group mygroup \
  --location eastus

az servicebus queue create \
  --name mysessionqueue \
  --namespace-name mynamespace \
  --resource-group mygroup \
  --requires-session true
```
:::
