---
slug: development-updates-05
title: "Topaz Weekly Pulse #5: Azure App Service, Virtual Machines, Storage SAS security, Key Vault AES keys, and a live Portal terminal"
authors: kamilmrzyglod
tags: [general, appservice, virtualmachines, storage, keyvault, portal]
---

*This week in Topaz: Azure App Service lands as a first-class service with Plans and Sites. Virtual Machines gain full CLI coverage. Storage gets end-to-end SAS and stored-access-policy enforcement. Key Vault now handles symmetric AES keys. The Portal grows a live CLI terminal and universal tag editing.*

{/* truncate */}


## Case 1: Azure App Service — Plans and Sites

The largest addition this week is a complete first implementation of Azure App Service. Both resource tiers are now live:

**App Service Plans** — the compute tier that backs web apps:
- **Create or Update**, **Get**, **Delete**, **List by resource group**, and **List by subscription**.
- **Restart all sites** on a plan via a dedicated action endpoint.
- ARM deployment support wired through `TemplateDeploymentOrchestrator`.
- Azure PowerShell and Terraform test coverage added for plan lifecycle.
- CLI commands (`create`, `get`, `list`, `delete`) for interactive use.

**App Service Sites (Web Apps)**:
- **Create or Update**, **Get**, **Delete**, **List by resource group**, and **List by subscription**.
- **Check name availability** — returns the standard ARM response shape so Terraform's pre-flight validation passes.
- **Site configuration** (`GET .../config/web`) — returns the full `SiteConfigProperties` model including runtime stack settings.
- **Get web app stacks** — the metadata endpoint queried by tools and CLIs to enumerate available runtime versions.
- **Publish profile** — `POST .../publishxml` returns a placeholder profile to satisfy SDK calls during deployment toolchain testing.
- `kind` defaults to `"app"` for sites created without an explicit value — required to prevent `az webapp list` from silently dropping the resource.
- `possibleOutboundIpAddresses` is always populated (empty string is valid) to prevent `az webapp show` from crashing on attribute access.

A dedicated `AppService` filter was added to the CI test matrix so these tests can run in isolation.


## Case 2: Virtual Machines — full lifecycle and image version queries

The Virtual Machine service that landed in skeleton form last week has reached full operational coverage this week:

**Endpoint coverage**:
- **Create or Update**, **Get**, **Delete**, **List by resource group**, and **List by subscription** are all implemented and wired to the ARM deployment orchestrator.
- **List VM image versions** (`GET .../providers/Microsoft.Compute/locations/{location}/publishers/{publisher}/artifactTypes/vmimage/offers/{offer}/skus/{sku}/versions`) returns the standard paged response shape.
- **Get a specific image version** by the full publisher/offer/sku/version path.

**CLI commands** — the Topaz CLI now exposes `vm create`, `vm show`, `vm list`, and `vm delete` commands, making it possible to script full VM lifecycle flows against the emulator without using the Azure SDK or Terraform directly.

Image version queries are the detail that most IaC tooling (especially Terraform's `azurerm_platform_image` data source) performs before creating a VM. Having these stubs in place removes a common blocker when running compute-heavy Terraform plans locally.


## Case 3: Storage SAS and stored access policies reach full enforcement

The Storage service has historically accepted SAS tokens without fully validating them — a gap that matters when testing auth-sensitive code paths. This week, end-to-end SAS enforcement was implemented across all three storage planes:

**Stored access policies**:
- `GET`, `PUT`, and `DELETE` stored access policy endpoints for Blob containers, Queues, and Tables.
- Policies persist to disk via the existing `ResourceProviderBase` infrastructure.
- **Revocation** — deleting a stored access policy immediately invalidates any outstanding SAS token that references it via the `si=` parameter.

**Account SAS validation** (`sv=`, `ss=`, `srt=`, `sp=`, `se=`, `spr=`, `sig=`):
- Signature computed over the canonical string-to-sign for the account key.
- Service (`ss=`), resource type (`srt=`), and permission (`sp=`) sets are enforced against the requested operation.

**Service SAS validation** (`sv=`, `sr=`, `sp=`, `se=`, `sig=`):
- Blob, Queue, and Table service SAS tokens are independently verified.
- When `si=` references a stored access policy, the policy's permissions, expiry, and start time are merged in before validation — exactly as Azure does.


## Case 4: Key Vault symmetric AES key operations

Key Vault's cryptographic data plane was previously limited to asymmetric key algorithms (RSA, EC). This week, symmetric AES keys (`kty=oct`) are fully supported:

- **Import** an AES-128, AES-192, or AES-256 key via the standard BYOK import path.
- **Encrypt** and **Decrypt** using:
  - `A128GCM`, `A192GCM`, `A256GCM` (AES-GCM with a random nonce and authentication tag).
  - `A128CBC`, `A192CBC`, `A256CBC` (AES-CBC with PKCS#7 padding).
- Request and response shapes follow the Key Vault REST API exactly, including the `iv`, `tag`, and `aad` fields for GCM modes.

Workloads that use Azure Key Vault for envelope encryption — wrapping per-record data keys with a customer-managed master key — can now run that full pattern end-to-end against Topaz.


## Case 5: Portal CLI terminal

The Topaz Portal gained a live, in-browser CLI terminal this week. The terminal is backed by a new `CliExecutionService` that proxies commands to the Topaz host binary, so every operation available in `topaz-host` is reachable directly from the browser UI.

Key behaviours:
- **Command history navigation** with Arrow-Up / Arrow-Down, matching the muscle memory users have from real terminals.
- **Contextual suggestions** — the `CliSuggestionService` analyses the partial command and surfaces both command names and required option placeholders, reducing lookup friction.
- **Host info integration** — the terminal panel fetches host metadata via the new `GetHostInfoAsync` endpoint (`HostInfoDto`) to display version and connectivity state.

The terminal component, suggestion service, and execution service are each covered by dedicated bUnit tests.


## Case 6: Universal tag editing in the Portal

Tags are a first-class concept in ARM but were previously read-only in the Topaz Portal. A shared `TagsPanel` Razor component was introduced this week and wired into every resource type that supports tagging:

- Event Hub Namespaces
- Key Vaults
- Managed Identities
- Resource Groups
- Service Bus Namespaces
- Storage Accounts
- Subscriptions
- Virtual Machines
- Virtual Networks

The panel allows adding, editing, and removing individual tags and persists changes back to the control plane via the subscription tag API. Component tests in `TagsPanelTests.cs` verify the add/edit/remove flows.

:::tip[Try what shipped this week]
Everything but emulation of Azure App Services runs in the current release — one binary, no Azure subscription.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Getting started →](https://topaz.thecloudtheory.com/docs/intro/) · Not ready to install? [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::
