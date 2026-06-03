---
slug: development-updates-03
title: "Topaz Weekly Pulse #3: Key Vault certificates, Management Groups maturity, and a new Portal dashboard"
authors: kamilmrzyglod
tags: [general, keyvault, managementgroups, storage, mcp, portal]
---

*This week in Topaz: Key Vault certificates land, Management Groups reach full CRUD maturity, Storage gets Entra ID auth, and the Portal grows a real dashboard.*

{/* truncate */}


## Case 1: Key Vault certificate management is now fully covered

The final pillar of the Key Vault data plane — certificates — landed this week. The following operations are implemented and wired to the CLI:

- **Create**, **Import**, **Get**, and **List** certificates.
- **List Versions** for a given certificate name.
- **Update** certificate attributes.
- **Delete** a certificate and **Get** the resulting certificate operation status.

Alongside the endpoint implementations, new model types were introduced for `CertificateBundle`, `CertificatePolicy`, and their associated request/response shapes. The KeyVault model layer was also refactored to organise Key, Secret, and Certificate models into consistent `Requests/` and `Responses/` sub-namespaces — improving maintainability and aligning serialization attributes for better API compatibility.

With certificates in place, Key Vault now covers all three data-plane asset classes (secrets, keys, certificates), making it suitable for end-to-end local development of workloads that rely on the full Key Vault SDK.

## Case 2: Management Groups reach operational maturity

Management Groups received a major expansion this week, going from basic CRUD to a feature-complete service:

- **Subscription association and disassociation** — add or remove subscriptions from a management group, with full E2E test coverage.
- **Get Entities** — query the entity graph across management groups and subscriptions at the tenant scope.
- **Get Descendants** — list all descendants (child groups and subscriptions) beneath a specific management group.
- **Hierarchy Settings** — full CRUD for tenant-level hierarchy settings: Create/Update, Get, List, Delete, and Update, each backed by a dedicated endpoint and persisted via the management group resource provider.
- **CLI commands** — Delete, List, Show, Update, and Remove Subscription commands added, along with a `ListDescendants` command.
- **Subscription auto-association** — subscriptions are now automatically linked to the management group hierarchy on initialisation via the event pipeline.

This makes Topaz a viable emulator for teams building policy-as-code tooling, governance automation, or anything that targets management group scopes via `az account management-group` or the Azure Management SDK.

## Case 3: Resource Provider Registration and enforcement

The ARM resource provider lifecycle is now enforced end-to-end:

- **List, Register, and Unregister** provider namespace operations are implemented and marked stable.
- The request **Router now checks provider registration state** before dispatching requests — unregistered providers receive an appropriate error response, mirroring Azure's behaviour.
- Endpoint classes for `Microsoft.Storage` and `Microsoft.Compute` declare their `ProviderNamespace` so the router can enforce registration automatically.
- Comprehensive E2E tests cover listing providers, registering, and unregistering, verifying that auto-registration on first use also works correctly.

This closes a category of subtle test failures where Terraform or the Azure SDK silently assumed a provider was already registered.

## Case 4: Entra ID authentication for Azure Storage data-plane

All three Azure Storage data-plane services — Blob, Queue, and Table — now support **Bearer token authentication** in addition to SharedKey:

- Dedicated `BlobStorageSecurityProvider` and `QueueStorageSecurityProvider` handle both SharedKey and Bearer token validation.
- `TableStorageSecurityProvider` was updated to use the same shared authorization checker.
- A new `StorageDataPlaneAuthorizationChecker` validates Bearer tokens against RBAC role assignments, delegating to the same authorization infrastructure used by Key Vault.
- E2E tests verify that both authentication paths work correctly and that unauthorized requests are rejected.

This matters for workloads that use `DefaultAzureCredential` with Storage — they can now authenticate via Entra ID against Topaz without switching to connection strings.

## Case 5: Service Bus topics and subscriptions added

The Service Bus data plane grew two new entity types this week:

- **Topics** — Create and Get endpoints, including control plane integration.
- **Subscriptions** (within topics) — Create and Get endpoints with dedicated request/response models.

These fill a gap for workloads that use the pub/sub model in Service Bus rather than simple queues, and bring the Service Bus surface meaningfully closer to the Azure REST API.

## Case 6: Portal dashboard is live

The Topaz Portal received its first real dashboard this week, replacing the placeholder home page with a set of functional widgets:

- **Resource Summary** — counts of resource groups, storage accounts, Key Vaults, and other resources at a glance.
- **Subscriptions** — lists active subscriptions with quick-link navigation.
- **Deployments** — shows recent ARM deployments and their status.
- **Quick Actions** — common provisioning shortcuts accessible from the home page.
- **Resource Groups** — an overview panel of all resource groups with counts.
- **Clock** — a live clock widget for the current time.

The dashboard is backed by bUnit component tests covering each widget's rendering and interaction behaviour, with 273 new test lines ensuring the layout holds up across different data states.

## Case 7: MCP server toolkit expansion

The MCP server — which exposes Topaz management as AI-callable tools for GitHub Copilot, Claude, and similar assistants — received a substantial expansion:

- **New provisioning tools**: `CreateContainerRegistryTool`, `CreateEventHubTool`, `CreateServiceBusTool`, `CreateStorageTool`, and `DeleteResourcesTool` join the existing set.
- **Status and diagnostics**: `GetTopazStatus` probes service health and returns live status for all emulated services. `GetConnectionStrings` retrieves ready-to-use connection strings for provisioned resources.
- **Prompts**: New Environment and Scenario prompts guide AI assistants through setting up complete Azure environments, including multi-resource topologies.
- **Docker integration**: `SetupTopazTool` now returns ready-to-paste Docker CLI commands for starting and stopping the Topaz container.

The `Topaz.MCP` documentation was also updated to reference available Docker image tags and document the prompt system.

## Case 8: Virtual Network subnets and NICs

Virtual Network support was extended with CRUD operations for **subnets** and **network interface cards (NICs)**. The `ResourceProviderDataResponse` was enhanced to carry richer data for these nested resource types, and the router was updated to dispatch the new endpoint patterns. Azure CLI and Azure PowerShell tests were also updated to cover the expanded surface.

## What to expect next

Key Vault certificate CLI commands (the last piece of the certificate story) are already committed and will appear in the next release. The Portal storage pages (containers, queues, tables) are scaffolded and ready for data binding. SAS validation and public access enforcement for Blob Storage are scoped in the backlog as the next storage security milestone.

:::tip[Try what shipped this week]
Everything in this edition runs in the current release — one binary, no Azure subscription.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Getting started →](https://topaz.thecloudtheory.com/docs/intro/) · Not ready to install? [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::
