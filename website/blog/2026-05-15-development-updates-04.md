---
slug: development-updates-04
title: "Topaz Weekly Pulse #4: OData Table queries, Virtual Network maturity, NSGs, and Dev Containers"
authors: kamilmrzyglod
tags: [general, storage, networking, keyvault, devcontainers, authorization]
---

*This week in Topaz: Table Storage learns OData, Virtual Networks reach full CRUD, Network Security Groups arrive, Key Vault gets auto-purge, and the repo ships an official Dev Container.*

{/* truncate */}


## Case 1: OData query support for Table Storage

Table Storage entity queries now support the full OData query protocol that Azure SDK clients expect:

- **`$filter`** — evaluate `eq`, `ne`, `gt`, `lt`, `ge`, `le`, `and`, `or`, and `not` expressions against any entity property, including typed comparisons for strings, integers, booleans, and `datetime` values.
- **`$select`** — return a subset of properties rather than the full entity payload.
- **`$top`** — limit the result count.
- **Pagination with continuation tokens** — `x-ms-continuation-NextPartitionKey` and `x-ms-continuation-NextRowKey` headers are emitted when a page boundary is hit, allowing callers to iterate through large result sets exactly as they would against Azure.

The implementation introduced a dedicated `TableODataFilter` parser and `TableODataQueryOptions` model, keeping the query logic well-separated from the storage data plane. End-to-end tests covering filter expressions, projections, and multi-page pagination were added for both the Azure SDK and the Azure CLI test suites.

This closes the most common compatibility gap teams hit when bringing real application code to Topaz for local testing.


## Case 2: Virtual Networks reach full CRUD maturity

The Virtual Network service received the remaining lifecycle operations it was missing:

- **Delete** a virtual network and its associated state.
- **List** virtual networks by resource group and by subscription.
- **Update tags** via a dedicated PATCH endpoint.
- **Check IP Address Availability** — validate whether a given IP address is free within a VNet's address space, with proper validation and structured response.

The control plane was updated to wire all new operations, and Azure CLI tests were added for each. Together with the subnet and VNet create/get operations shipped last week, the service now covers the full day-to-day VNet management lifecycle.


## Case 3: Network Security Groups arrive

Network Security Groups are a new resource type in Topaz this week, fully integrated into the Virtual Network service:

- **Create or Update**, **Get**, **Delete**, and **List** (by resource group and by subscription) NSG endpoints.
- ARM deployment orchestrator updated to route `Microsoft.Network/networkSecurityGroups` deployments.
- Azure CLI, Azure PowerShell, and Terraform test coverage added for create and retrieval.
- API coverage documentation updated.

NSGs are a foundational dependency for realistic network topology emulation — teams building infrastructure-as-code that wires subnets to security groups can now validate that logic end-to-end without touching Azure.


## Case 4: Key Vault soft-delete auto-purge

Key Vault's soft-delete lifecycle is now fully automated:

- A **`KeyVaultSecretsSoftDeletePurgeScheduler`** background service scans soft-deleted secrets and purges those whose retention period has elapsed.
- This joins the existing **vault-level purge scheduler** introduced in a prior commit, giving the full soft-delete lifecycle — delete, recover, and time-based auto-purge — for both vault containers and individual secrets.
- A new **`BackgroundServiceOrchestrator`** in `Topaz.Host` provides a unified startup/shutdown mechanism for all registered `ITopazBackgroundService` implementations, replacing ad-hoc host lifetime hooks.
- E2E tests verify that purge scheduling and recovery interact correctly.

For workloads that test secrets rotation or deletion-triggered cleanup logic, the emulator now behaves the same as a real Key Vault with default retention settings.


## Case 5: Role assignment propagation across all ARM scopes

The authorization service was substantially expanded this week to cover all ARM scopes where role assignments can be created:

- **Management group scope** — `PUT /providers/Microsoft.Management/managementGroups/{id}/providers/Microsoft.Authorization/roleAssignments/{name}`.
- **Resource group scope** — `PUT /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Authorization/roleAssignments/{name}`.
- **Individual resource scope** — `PUT` targeting any arbitrary resource path.

A **propagation model** was introduced: assignments made at a broader scope are automatically visible when checking permissions at a narrower scope, mirroring Azure's inherited RBAC semantics. `TopazArmClient` — the in-process ARM client used by E2E tests — was expanded with helper methods for all three scopes, and a comprehensive E2E test suite (~225 lines) was added to cover assignment creation and cross-scope visibility.


## Case 6: Official Dev Container support

Topaz now ships with a fully working `.devcontainer` configuration:

- **DNS sidecar** — a Technitium DNS container is included in the Compose setup and configured to resolve `*.topaz.local.dev` wildcard entries, so all Azure SDK calls route to the local emulator automatically.
- **TLS certificate trust** — the `postCreate` script generates Topaz's self-signed certificate and installs it into both the system trust store and the Azure CLI certificate store, so HTTPS clients work without extra configuration.
- **Topaz CLI bootstrapped automatically** — the setup script detects the current platform architecture, downloads the matching CLI binary, and places it on `$PATH`.
- **Azure CLI pre-configured** — the devcontainer image extends the official `mcr.microsoft.com/devcontainers/universal` image and layers in all the environment variables (`AZURE_CLI_DISABLE_CONNECTION_VERIFICATION`, `REQUESTS_CA_BUNDLE`) that make `az` commands work against the local emulator.
- **README badge** — the repo homepage now carries a "Dev Containers" badge linking to the configuration.

The companion blog post ([Dev Container for Topaz](/blog/devcontainer-topaz)) covers the full setup in detail for teams that want to understand what each piece does.


## Case 7: Secondary storage endpoints

Azure Storage accounts expose two sets of endpoints: a primary endpoint for normal traffic and a secondary read-access endpoint for geo-redundant accounts. The storage control plane now returns a correctly shaped `secondaryEndpoints` object in account responses, with static helper methods generating both primary and secondary endpoint URLs from a single account name. This fixes a class of client-side errors where SDKs that inspect the account descriptor before connecting would fail when the secondary entry was absent or malformed.


## Case 8: Security hardening and legacy client compatibility

Two lower-profile but important correctness fixes landed this week:

- **Storage path validation** — the blob and queue storage endpoints now validate and sanitize path parameters before using them in filesystem operations, eliminating path traversal vectors that static analysis tools had flagged. Container and blob name rules are enforced at the boundary.
- **Key Vault 401 responses for legacy clients** — 401 challenges from Key Vault data-plane endpoints now include a parseable JSON error body alongside the `WWW-Authenticate` header. Older Azure SDK clients based on `go-autorest` would treat a header-only 401 as a hard EOF and crash; the structured body prevents that. The token endpoint also gained additional route patterns to handle legacy token acquisition flows.


## What to expect next

Virtual Network IP address allocation registry is scoped in the backlog as the next networking milestone. Azure App Service control plane features are on the roadmap for an upcoming release. Key Vault certificate CLI commands are committed and will ship in the next release alongside certificate management improvements.

:::tip[Try what shipped this week]
Everything in this edition runs in the current release — one binary, no Azure subscription.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Getting started →](https://topaz.thecloudtheory.com/docs/intro) · Not ready to install? [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::
