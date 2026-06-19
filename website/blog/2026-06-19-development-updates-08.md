---
slug: development-updates-08
title: "Topaz Weekly Pulse #8: Cosmos DB Data Plane Queries, Device Code Authentication, Service Bus Authorization Rules, and Documentation Expansion"
authors: topaz
tags: [general, cosmos-db, entra, service-bus, documentation]
---

*This week in Topaz: Azure Cosmos DB gains full SQL query execution with schema-aware document CRUD, partition range planning, and QueryEngine refactoring. Entra ID adds device code authentication with an interactive HTML authorization flow for headless and CLI scenarios. Service Bus introduces authorization rules for namespace and entity-level access control. Documentation and onboarding receive major updates, including architecture guides, installation improvements, and wildcard DNS support for certificate generation.*

{/* truncate */}


## Case 1: Azure Cosmos DB — SQL Query Execution and Data Plane Maturity

Azure Cosmos DB's data plane reached production readiness this week with full SQL query support, document lifecycle management, and Azure SDK compatibility.

**SQL Query Engine**:
- **Cosmos DB SQL Parser** — parses and validates SQL queries against schemas, supporting `SELECT`, `WHERE`, `ORDER BY`, `OFFSET/LIMIT`, and projection clauses.
- **Query Execution** — queries execute against live documents using `CosmosClient` and `QueryDefinition`, matching the Azure SDK API contract exactly.
- **Partition Key Ranges** — `GetPartitionKeyRangesEndpoint` is now available, allowing SDKs and tools to discover partition topology before query execution. Query plans are computed based on the requested document schema and partition key path.
- **QueryEngineConfiguration** — configuration model persisted in `AccountPropertiesResponse`, allowing test code to inspect query engine settings and behavior.

**Document CRUD**:
- **Create, Read, Update, Delete** — full document lifecycle in `CosmosDbService` with automatic ID generation and conflict handling.
- **Throughput Tracking** — response headers include the standard `x-ms-request-charge` to allow SDKs to track RU consumption per operation.
- **Soft Delete Semantics** — document soft-delete (`isDeleted` flag) support for TTL and compliance scenarios.

**End-to-End Compatibility**:
- E2E tests verify that Azure SDK `CosmosClient` calls (document operations, queries, throughput checks) work against Topaz without modification.
- Known limitations (such as hardcoded `x-ms-request-charge` in response headers) are documented, making it clear what behavior may differ from production Azure in edge cases.
- All document CRUD operations integrate with Topaz's resource persistence layer, ensuring that data survives emulator restarts within a test session.


## Case 2: Entra ID — Device Code Authentication Flow

User authentication flows received a major addition this week: the **device code** (user authorization code) flow for CLI and headless application scenarios.

**Device Code Flow**:
- `POST /oauth2/v2.0/devicecode` generates a device code and user code, returning both along with a verification URI that users can open in a browser.
- Parameters: `client_id`, `scope`, `tenant_id` (optional).
- Emulator generates unique codes and tracks pending authorizations in memory.

**Interactive Authorization**:
- A new **HTML authorization interface** (`/authorize`) renders the device code verification page that users can access in a browser.
- Users enter their device code, confirm the requested scope, and authorize the client application.
- The authorization state is stored temporarily and polled by CLI commands waiting for completion.

**CLI Integration**:
- Azure CLI and SDK CLI tools can now run `az login --allow-no-subscriptions` and authenticate interactively against Topaz, using the device code flow instead of requiring bearer tokens or user credentials.
- Useful for testing headless authentication scenarios, CI/CD pipelines that use device code, and developer workflows where interactive login is expected.

**Backward Compatibility**:
- Existing ROPC (Resource Owner Password Credential) flows continue to work unchanged.
- Authorization code flows and service principal authentication are still supported.

The device code flow unblocks testing of Azure CLI login scenarios and interactive headless applications without a live Microsoft Entra ID tenant.


## Case 3: Service Bus — Authorization Rules with Full CRUD

Service Bus authorization rules are now fully implemented, enabling namespace-level and entity-level access control testing.

**Authorization Rule Management**:
- **Create or Update** — `PUT /namespaces/{namespace}/AuthorizationRules/{name}` persists named rules with `Listen`, `Send`, `Manage` rights.
- **Get, Delete, List** — retrieve and manage rules at the namespace level.
- **Entity-Level Rules** — rules can be attached to topics and queues for fine-grained access control.
- **Keys and Connection Strings** — `POST .../listKeys` and `POST .../listConnectionStrings` return primary and secondary keys with full SAS generation.

**Access Control Response Model**:
- Authorization rule responses include `keyName`, `primaryKey`, `secondaryKey`, `rights` (as an array of `Listen`, `Send`, `Manage`), and connection strings.
- Full compliance with Azure Service Bus authorization rule contracts.

**Test Infrastructure**:
- Service Bus SDK tests verify that authorization rules can be created, listed, and queried via ARM API.
- Terraform tests confirm that Terraform's `azurerm_servicebus_namespace_authorization_rule` resource works against Topaz.
- CLI tests validate `az servicebus namespace authorization-rule` commands.


## Case 4: Documentation and Onboarding Expansion

The documentation and onboarding experience received significant investment this week.

**Architecture Documentation**:
- New comprehensive guide to Topaz's internal architecture, including service emulation patterns, request routing, resource persistence, and testing strategies.
- Explains how services are organized, how the Router works, and how to extend Topaz with new services.
- Designed for contributors and advanced users who want to understand the internals.

**Installation and Quick Start**:
- Installation notes layout improved with better visual hierarchy and clearer next-step links.
- README highlights updated to emphasize self-contained binary and local-first development workflow.
- Quick start links in the main README now guide new users toward the right documentation based on their use case (Docker, Homebrew on macOS, direct download on Linux).

**DNS and Certificate Infrastructure**:
- Certificate generation script now supports **wildcard DNS entries**, simplifying test environments where multiple services need to be accessed via `*.vault.topaz.local.dev`, `*.servicebus.topaz.local.dev`, etc.
- Certificate generation is automatically triggered at startup when running outside Docker.
- Wildcard certificates reduce the number of individual hostnames needed during local testing.

**Onboarding Components**:
- Call-to-action (CTA) component updated to highlight local operation and future tier plan options.
- Links to Cosmos DB documentation, CI/CD integration examples, and community resources added to guide users toward relevant next steps.


## Case 5: Network and Infrastructure Improvements

Several infrastructure improvements round out this week's changes.

**SAS Token Generation**:
- SAS token IP address retrieval now uses Python instead of shell commands, improving cross-platform compatibility and error handling.
- Token generation is more robust in environments with complex network configurations.

**Error Response Handling**:
- Service Bus topic operations now correctly return `HttpStatusCode.NotFound` (404) instead of generic server errors when resources don't exist.
- Improves error diagnostics and matches Azure's error contract more closely.

**Load Balancer Improvements**:
- Load Balancer tests refactored to assert on properties directly from API responses.
- Full object patching for tags now works correctly, allowing test code to update load balancer tags without re-creating the resource.


## Summary: Getting Closer to Full Feature Parity

This week consolidates Topaz's position as a realistic local Azure emulator. Cosmos DB now supports end-to-end application workflows (resource provisioning + document operations + queries). Entra ID authentication covers more real-world flows (device code, service principals, ROPC). Service Bus is now a fully-featured messaging backbone for testing event-driven systems.

The documentation expansion—particularly the architecture guide and installation improvements—reflects growing community adoption and the need for clear paths to contribution and extension.


:::tip[Try Topaz with Full Cosmos DB and Device Code Auth]
Everything runs in a single binary — no Azure subscription required.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

Authenticate with device code flow:

```bash
az cloud register -n topaz --endpoint-resource-manager https://topaz.local.dev:8899 \
  --endpoint-active-directory https://topaz.local.dev:8899 \
  --endpoint-active-directory-graph-resource-id https://topaz.local.dev:8899

az cloud set -n topaz
az login --allow-no-subscriptions
# Follow the device code URL in your browser
```

Create and query a Cosmos DB database:

```bash
export SUBSCRIPTION_ID=$(az account show --query id -o tsv)
export RESOURCE_GROUP="mygroup"
export LOCATION="eastus"

az group create -n $RESOURCE_GROUP -l $LOCATION

az cosmosdb create \
  --name "myaccount" \
  --resource-group $RESOURCE_GROUP \
  --locations regionName=$LOCATION failoverPriority=0

az cosmosdb sql database create \
  --account-name "myaccount" \
  --resource-group $RESOURCE_GROUP \
  --name "mydb"

az cosmosdb sql container create \
  --account-name "myaccount" \
  --database-name "mydb" \
  --resource-group $RESOURCE_GROUP \
  --name "items" \
  --partition-key-path "/id"

# Use Azure SDK to insert and query documents
```

[Getting started →](https://topaz.thecloudtheory.com/docs/intro/) · [Cosmos DB docs →](https://topaz.thecloudtheory.com/docs/api-coverage/cosmos-db/) · [Service Bus docs →](https://topaz.thecloudtheory.com/docs/api-coverage/service-bus/) · [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::
