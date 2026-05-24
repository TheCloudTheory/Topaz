---
slug: topaz-vs-azurite
title: "Topaz vs Azurite: what actually works locally and what doesn't"
description: An honest comparison of Topaz and Azurite for local Azure development. Storage parity, Key Vault soft-delete, Service Bus AMQP, Container Registry, Entra ID, RBAC, ARM templates, Terraform, MCP — what each tool emulates, what neither does yet, and when to pick which.
keywords: [topaz vs azurite, azurite alternative, local azure emulator, azure key vault local, service bus emulator, azure container registry local, azure entra id emulator, terraform azurerm local, azurite limitations]
authors: kamilmrzyglod
tags: [general, storage, entra, terraform]
---

If you have ever written a line of Azure code on a laptop, you have used Azurite. It is the official local emulator for Azure Storage, ships in every Visual Studio install, and runs unchanged in tens of thousands of CI pipelines. For Storage-only workloads it is an excellent tool — Microsoft maintains it, Azure SDKs target it, and the parity with the real Azure Storage REST API is strong.

The problem is that real applications stop at Azure Storage roughly never. The moment you reach for a secret in Key Vault, publish a message to Service Bus, push an image to a Container Registry, or want a `DefaultAzureCredential` chain that does not silently fall back to interactive browser auth, Azurite has nothing to offer. You are left bolting together a Service Bus emulator from a community Docker image, mocking the Key Vault SDK in tests, and hoping that the way your CI fakes Entra tokens does not drift away from how production behaves.

Topaz is a single .NET 10 binary that emulates Azure Storage, Key Vault, Service Bus, Event Hubs, Container Registry, Managed Identity, RBAC, ARM, and a working Entra ID layer in one process. This post is an honest comparison between the two, focused on what developers who already know Azurite actually run into.

{/* truncate */}

:::note[Updated — v1.4 released (19 May 2026) · v1.5 User Delegation SAS]
Several Storage gaps described in this post were addressed in v1.4: **SAS token validation** (Account SAS, Service SAS, stored access policies), **public-access Blob reads**, and **RA-GRS secondary endpoint** DNS registration, stats, and read-only enforcement are all live. In v1.5, **User Delegation SAS** for Blob Storage shipped — `generateUserDelegationKey` requires Bearer (Entra) auth, derives the delegation key deterministically from the account key and the caller's OID/TID, and the data plane validates User Delegation SAS tokens end-to-end. See the Update sections below and the [roadmap](/roadmap) for current status.
:::

## Where Topaz and Azurite agree

It is worth being clear about this up front: for Azure Storage, Azurite is good. Anyone telling you otherwise is selling something. Topaz does not exist because Azurite is bad at Storage — it exists because Azurite is *only* Storage, and most real Azure applications are not.

For Blob, Queue, and Table data plane operations, the two emulators are at full parity on the cases that matter for day-to-day development:

| Storage feature | Topaz | Azurite |
|---|---|---|
| Blob basic operations (put, get, delete, head, list) | Yes | Yes |
| Blob metadata, container metadata | Yes | Yes |
| Block blobs (`Put Block`, `Put Block List`, `Get Block List`) | Yes | Yes |
| Page blobs (`Put Page`, `Get Page Ranges`) | Yes | Yes |
| Container ACLs and stored access policies | Yes | Yes |
| Container and blob leases (acquire / renew / change / release / break) | Yes | Yes |
| Blob copy and snapshots | Yes | Yes |
| Table create / delete / query / entity CRUD | Yes (stable) | Yes (preview) |
| Table OData queries ($filter, $select, $top, $skiptoken) | Yes | Yes |
| Table ACL (stored access policies) | Yes | Yes |
| Queue messages (enqueue, dequeue, peek, update, delete, clear) | Yes | Yes |
| Queue ACL | Yes | Yes |
| RA-GRS secondary endpoints | Partial — DNS, stats, read-only enforcement ✅ (v1.4); general reads on secondary (v1.6) | Yes |

As of v1.4, SAS token validation (Account SAS and Service SAS), stored access policy enforcement, and public-access Blob reads have shipped on the data plane. RA-GRS secondary endpoint DNS registration, `GetServiceStats`, and read-only enforcement are also live; general data reads through secondary endpoints follow in v1.6. See the Update section at the end of this post for a full summary.

The Storage feature that Azurite still does not have is *multiple named storage accounts as a first-class concept*. Azurite's default account is `devstoreaccount1`. Adding more requires editing your hosts file, exporting `AZURITE_ACCOUNTS` with `name:key1:key2;name:key1:key2`, and restarting the emulator. Topaz has a real ARM control plane: `az storage account create --name sa-orders --resource-group rg-local` works the same way it works against Azure, registers a DNS entry automatically, and gives you a real connection string you can paste into Azure Storage Explorer or hand to Terraform.

## Where Azurite stops: Key Vault

This is the largest single delta between the two emulators, and it is the reason most developers eventually try to replace Azurite.

Azurite has no Key Vault. Not a partial one, not a stubbed one — none. The standard workarounds are:

- Mock the `SecretClient` in tests (loses any coverage of the actual auth layer).
- Read secrets from `appsettings.Development.json` instead (creates a code-path divergence between local and production).
- Spin up a real Key Vault per developer (works, but now CI needs Azure credentials and the developer onboarding doc has a "before you can run tests, ask the platform team for..." section).

Topaz has the most complete Key Vault emulation of any service it ships. The control plane covers the full vault lifecycle — create, update, soft-delete, list deleted, recover, purge, name availability, access policy management — and the data plane runs on its own port (`8898`) reachable at `https://<vault-name>.vault.topaz.local.dev:8898/`, which is the URL pattern the Azure SDK already constructs.

What works today on the data plane:

| Surface | Operations |
|---|---|
| **Secrets** | Set, Get (by name and version), List, Update, Delete, Get Versions, Backup, Restore, full soft-delete surface (Get Deleted, List Deleted, Recover, Purge) |
| **Keys** | Create, Import (RSA + EC), Get, List, Update, Delete, Backup, Restore, Rotate, Get/Update Rotation Policy, full soft-delete surface, `encrypt`, `decrypt`, `sign`, `verify`, `wrapKey`, `unwrapKey`, `release`, `Get Random Bytes`, `Get Key Attestation` |
| **Certificates** | Full surface — create, import, get, update, delete, list, versions, backup/restore, contacts, issuers, pending operations, merge, and soft-delete (implemented in v1.3) |

The soft-delete and recovery surface deserves a specific call-out because it is what most teams accidentally depend on. If your application catches `KeyVaultErrorException` on a soft-deleted secret, recovers it, and continues — that code path is unreachable in any other local emulator. Topaz exercises it end-to-end. Tokens are real signed JWTs. The vault URL works with `DefaultAzureCredential` exactly as in production.

The full certificate surface was implemented in v1.3 — CRUD, soft-delete, issuers, contacts, pending operations, and merge are all covered. As of v1.4, AES symmetric key (`oct`) cryptographic operations (AES-GCM and AES-CBC) are also supported.

## Where Azurite stops: Service Bus

Service Bus is the second most common reason teams outgrow Azurite. The community workarounds — RabbitMQ, ActiveMQ, in-memory test doubles — all break the same invariant: the AMQP wire format is not the same, so any code that reaches into broker-specific behaviour (dead-letter queues, peek-lock vs receive-and-delete, deferred messages, session state) drifts away from Service Bus reality.

Topaz runs a real AMQP 1.0 broker on ports `8889` (plain) and `5671` (AMQP/TLS), implemented natively in the host process. The Azure Service Bus SDK connects to it without modification. MassTransit, NServiceBus, and any other library that speaks AMQP work because they are speaking AMQP, not because Topaz pretends to be Service Bus through a thin shim.

| Surface | What works |
|---|---|
| Namespaces (control plane) | Create / Update / Delete / Get / List By Resource Group |
| Queues (control plane) | Create / Update / Delete / Get / List By Namespace |
| Topics (control plane) | Create / Update / Delete / Get / List By Namespace |
| Subscriptions | Create / Update / Delete / Get (via AMQP management node) |
| Messaging (data plane, AMQP) | Send / Receive on queues and topics, Complete / Abandon / Dead-letter |
| AMQP entity management | Create / Get / Delete queues, topics, subscriptions — used by MassTransit |

Specifically *not* implemented yet: subscription rule management (filters / actions), namespace authorization rules, list-keys / regenerate-keys, disaster recovery configs, migration configs, private endpoints. For most application development workflows this is a non-issue; if you are building tooling that programmatically rotates Service Bus SAS keys or manages DR pairing, real Azure is still the right target for those tests.

## Where Azurite stops: Container Registry

`docker push myregistry.azurecr.io/myapp:latest` against a local emulator is the kind of thing that historically required either a real ACR (and therefore Azure credentials in CI) or running a generic Docker registry and pretending it is ACR (works for `pull`, breaks the moment anything touches the ARM control plane or the ACR-specific OAuth2 exchange).

Topaz emulates both layers:

- **Control plane** (ARM): create, update, delete, list registries; manage admin credentials; toggle `adminUserEnabled`.
- **Data plane** (OCI Distribution Spec, port `8892`): manifest CRUD, blob upload (`POST` / `PATCH` / `PUT` chunked uploads), blob download and existence checks, repository catalog, tag listing, and the ACR-specific `/oauth2/exchange` endpoint that makes `az acr login` work.

The full authentication flow — `GET /v2/` returning a `Www-Authenticate` challenge, `POST /oauth2/exchange` swapping the Entra token for an ACR refresh token, and the bearer token round-trip — is implemented end-to-end. There is a [separate post on how this works](/blog/acr-data-plane) that goes into the design tradeoffs.

What this unlocks: you can run `docker push`, `helm push`, and any OCI-compliant client against Topaz without modifying anything in your build pipeline. The registry hostname follows real Azure conventions (`myregistry.cr.topaz.local.dev:8892`). Image promotion workflows, CI builds that publish images, and Terraform configurations that create ACRs all work locally. `docker pull` and end-to-end image pull-through is on the roadmap.

## Where Azurite stops: Entra ID and Managed Identity

This is the silent one. Azurite does not need an identity layer because it accepts SharedKey for authentication and falls back to "no auth" when nothing is provided. The moment your application uses `DefaultAzureCredential` — which is the Microsoft-recommended pattern for everything except Storage — Azurite cannot help. Your local code either talks to a real Entra tenant (forcing every developer onto a corporate identity) or you replace `DefaultAzureCredential` with a custom test double (creating a code-path divergence with production).

Topaz ships a working Entra ID emulation layer. A local tenant (`topaz.local.dev`, tenant ID `50717675-3E5E-4A1E-8CB5-C62D8BE8CA48`) is provisioned at startup with a built-in superadmin account. Every token is a real, signed JWT — same format Azure issues, same claims layout, signed with HMAC-SHA256, one-hour lifetime. The OIDC discovery endpoint (`/.well-known/openid-configuration`) is fully functional, both `/organizations/` and `/{tenantId}/` variants are served, and four grant types are supported: `client_credentials`, `password`, `authorization_code`, and `refresh_token`.

Tied to that:

- **Microsoft Graph API** for users, applications, service principals, and groups — enough to script a full identity setup.
- **Managed Identity** — user-assigned and system-assigned, including federated identity credentials. The same `DefaultAzureCredential` chain that works in production works locally, no special credential type required.
- **RBAC** — role assignments and role definitions at any ARM scope, with the standard built-in roles (Owner, Contributor, Reader, plus the service-specific data-plane roles like `Storage Blob Data Contributor`) pre-loaded.

This is the difference between "I can read a blob locally" and "I can run my entire authn/authz path locally". The latter catches a class of bug — token audience mismatches, role assignment drift, scope errors — that mocks never catch.

There is a [dedicated post on the Entra emulation layer](/blog/entra-id-emulation) if you want the design details.

## Where Azurite stops: ARM, Terraform, and the Azure CLI

Azurite has no ARM control plane, which means there is no `az group create`, no `azurerm_resource_group`, no Bicep deployment, no way to express the resources your application needs in the same infrastructure-as-code language you use in production. Local development and production end up using two different definitions of "what infrastructure exists".

Topaz has a working ARM emulation. The Resource Manager port (`8899`) accepts `az` and `azurerm` provider traffic, exposes a metadata document at the discovery endpoint that points every Azure API URL at the local emulator, and accepts ARM template / Bicep deployments end-to-end. The same `terraform apply` that creates resources in Azure can create them in Topaz with one provider setting:

```hcl
provider "azurerm" {
  features {}
  metadata_host                   = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}
```

That is the entire integration. `metadata_host` redirects endpoint discovery; the AzureRM provider then constructs every subsequent URL — Storage, Key Vault, Service Bus, Container Registry — from Topaz's metadata document. Detail in the [Terraform integration post](/blog/terraform-local-azure-no-subscription).

The Azure CLI works the same way: register Topaz as a cloud environment with `az cloud register`, switch to it, and `az login` issues a token from the local Entra layer. From there, `az keyvault secret set`, `az servicebus queue create`, `az acr login`, `az group deployment create` all work as they do against Azure. Azurite supports `az storage` and only `az storage`.

## What Azurite still does better

A genuinely honest post needs this section. Azurite is not strictly worse — it is narrower, more mature in its narrow scope, and the right call for some workloads.

- **Microsoft maintenance.** Azurite is shipped and supported by Microsoft. Topaz is open source and maintained by an independent team. If your organisation requires vendor-supported software for local development tooling, that distinction matters.
- **RA-GRS secondary endpoints.** Azurite emulates the full RA-GRS secondary URL pattern. Topaz added partial RA-GRS support in v1.4 — secondary DNS hostnames, `GeoReplicationStats` payloads, and read-only enforcement are live; general data reads through secondary endpoints are on the v1.6 roadmap.
- **The `UseDevelopmentStorage=true` shortcut.** Azurite is hardcoded to the Azure SDK's `UseDevelopmentStorage=true` connection string. Topaz uses real connection strings — the same format Azure issues — which is more flexible but loses the one-line shortcut.
- **Visual Studio integration.** Azurite ships with Visual Studio and Storage Explorer has a built-in "Local Emulator" entry. Topaz works with Storage Explorer, but you connect via a connection string rather than the dedicated emulator option.
- **Maturity.** Azurite has been in production CI pipelines for years. Topaz is in beta as of v1.0 and is moving fast — that is also the reason this post needs to list which surfaces are not yet covered.

If your application uses only Storage, a single account is enough, and you do not need ARM or Terraform integration locally, Azurite is genuinely the simpler choice and there is no reason to switch.

## What is coming for Storage in Topaz

The Storage roadmap is publicly tracked in [`BACKLOG.md`](https://github.com/TheCloudTheory/Topaz/blob/main/BACKLOG.md) and mirrored on the [website roadmap](/roadmap). The v1.4 items below have shipped — see the [roadmap](/roadmap) for current milestone status.

**✅ v1.4 — SAS token validation on the data plane.** _(shipped)_
The control plane already generates SAS tokens via `ListAccountSas` and `ListServiceSas`. The data-plane security providers (Blob, Queue, Table) currently only recognise the `Authorization:` header — incoming requests with `?sv=...&sig=...` query strings hit the missing-header path and return `401`. The work in progress adds:

- Account SAS validation: detects `sv`, `ss`, `srt`, `sp`, `se`, `st`, `spr`, `sip`, `sig`, builds the canonical Account SAS string-to-sign, HMAC-SHA256 against the account key, validates expiry / service / resource type / HTTP method.
- Service SAS validation: per-service string-to-sign, including the canonicalized resource and the response-header overrides (`rscc`, `rscd`, etc.), with stored access policy lookup when `si=<policyId>` is present.
- Stored access policy enforcement: the Container ACL / Queue ACL / Table ACL endpoints already round-trip `<SignedIdentifiers>` XML to disk; v1.4 wires them into the SAS validation path so revoking a named policy actually revokes the tokens that reference it.

**✅ v1.4 — anonymous / public-access reads for Blob containers.** _(shipped)_
Real Azure allows containers created with `x-ms-blob-public-access: container` (list + read) or `blob` (read only) to permit unauthenticated reads. Topaz currently rejects every request without an `Authorization` header. The fix is to store the public-access level on the container, look it up in the security provider when no auth is present, and permit the request when the level allows the operation.

**✅ v1.4 — RA-GRS secondary endpoint semantics.** _(partial, shipped)_
Secondary DNS hostnames (`{accountName}-secondary.*`), `secondaryEndpoints.blob/.table/.queue/.file` in the storage account ARM response, the `?restype=service&comp=stats` endpoint returning a realistic `GeoReplicationStats` payload, and read-only enforcement that returns `403 WriteOperationNotSupportedOnSecondary` on mutating requests against a secondary endpoint. General data reads through secondary endpoints follow in v1.6.

**✅ v1.5 — User Delegation SAS for Blob.** _(shipped)_
The Entra-derived SAS variant. Two coordinated pieces: an ARM endpoint that mints a user delegation key bounded by `(start, expiry, oid, tid)`, and Blob data-plane validation that recomputes the key from the stored fields and validates the signed token. This is the only SAS variant that needs the local Entra layer to be coherent with the storage layer — which is part of why Topaz is built as one process rather than five.

**v1.6-beta — unified data-plane port.**
Real Azure exposes Blob / Table / Queue / File on port 443 with subdomain-based routing (`{account}.blob.core.windows.net`, etc.). Topaz currently uses separate ports per sub-service (8891 / 8890 / 8893 / 8894) for routing simplicity. Some Azure CLI / SDK code paths construct storage URLs via `get_account_url()`, which builds a single `https://` URL from the cloud-suffix `storage_endpoint` — encoding only one port. Consolidating onto a single port behind subdomain routing fixes that and makes the local URL pattern identical to production.

These are all in the [public backlog](https://github.com/TheCloudTheory/Topaz/blob/main/BACKLOG.md) with milestone labels — there is no roadmap kept in someone's head.

## Developer experience: where the differences compound

The feature comparison above is the headline. The day-to-day experience matters more.

### Single binary, single process, single working directory

Azurite is one process for Storage. Replicating Topaz's surface with separate emulators means orchestrating Azurite + a Service Bus emulator + a registry + a mock identity server, all writing to different directories, all logging in different formats, all started from different scripts. Every team that goes down this path eventually writes a Docker Compose file that nobody is happy with.

Topaz is one binary or one Docker image. State lives in `.topaz/` next to your project. Stop the host, the state is preserved. Start it again, the state comes back. Delete the directory, the slate is clean. There is one log stream and one health check (`GET /health`).

### MCP server for AI tooling

Topaz ships an MCP server (`Topaz.MCP`) that exposes the host as a set of tools any MCP-compatible client can call. The intended use case is GitHub Copilot in VS Code — drop a `.vscode/mcp.json` in your workspace, point it at the MCP binary, and Copilot can run `RunTopazAsContainer`, `CreateSubscription`, `CreateResourceGroup`, `CreateKeyVault`, `CreateStorageAccount`, `CreateServiceBusNamespace`, `CreateContainerRegistry`, and a `GetConnectionStrings` tool that returns ready-to-paste connection strings for everything it just provisioned.

In practice this means you can describe a local environment in natural language:

> Start Topaz. Create a subscription, a resource group `rg-dev` in `westeurope`, then provision a Storage account, a Service Bus namespace with a queue named `orders`, and a Key Vault with a secret `db-password`. Output the connection strings.

…and the assistant runs the whole sequence. There is also a `GetTopazStatus` diagnostics tool for the case where a setup fails partway through and you want to know which ports are bound. Full details in the [MCP server documentation](/docs/mcp-server).

Azurite has no equivalent. There is no MCP integration, no programmatic way for an AI assistant to provision a Storage account, no notion of provisioning at all — the tool exists at the data plane and stops there.

### Editor and IDE integration

GitHub Copilot in VS Code works through the MCP server. The Topaz CLI (`topaz`) is a thin client over the host process — every command is `topaz <verb>` with consistent JSON output, which means it scripts cleanly from a terminal, a shell hook, a Makefile, or a CI step. A native VS Code extension is on the roadmap; for the moment, the MCP path covers the AI-assisted workflows and the CLI covers everything else.

### Scaling out: CI and Docker

Topaz publishes a Docker image (`topaz/host`) that runs as a sidecar in any CI environment. The pattern in GitHub Actions is:

```yaml
services:
  topaz:
    image: topaz/host:latest
    ports:
      - 8899:8899
      - 8898:8898
      - 8891:8891
      - 8889:8889
      - 8892:8892
    options: --health-cmd="curl -f http://localhost:8899/health || exit 1"
```

Then `terraform apply` against `metadata_host = "topaz:8899"` from any step. No Azure credentials in the pipeline secrets, no rate-limited subscription, no per-run cost. The same image runs as a Testcontainer inside .NET integration tests via the Topaz Testcontainers helper. The [CI/CD integration guide](/docs/ecosystem/ci-cd) covers GitHub Actions and Azure DevOps.

Azurite has a Docker image too. The difference is what the image emulates — Topaz's image covers the same surface this post described (Storage + Key Vault + Service Bus + Event Hubs + ACR + Entra + ARM); Azurite's covers Storage. If your CI only needs Storage, that is fine. If it needs more, Azurite forces you back to the multi-emulator orchestration problem.

### ASP.NET Core integration

`AddTopaz()` is an extension method on `IServiceCollection` that provisions local Azure infrastructure at application startup — declaratively, in the same `Program.cs` where you wire up DI. Spin up a resource group, a Service Bus namespace, a Key Vault with seed secrets, a Storage account, all in code, all conditional on environment so the same `Program.cs` runs unchanged in production. Detail in the [ASP.NET Core integration guide](/docs/ecosystem/aspnet-core).

## When to keep Azurite

- Your application uses only Azure Storage and a single account is sufficient.
- You need complete RA-GRS secondary endpoint support including general data reads through secondary endpoints (Topaz has partial RA-GRS support as of v1.4; general reads are on the v1.6 roadmap).
- You need a Microsoft-supported emulator with vendor backing.
- Your toolchain is built around Azurite and migration is not worth the effort.

## When to switch to Topaz

- Your application uses any service beyond Storage — Key Vault, Service Bus, Event Hubs, Container Registry, Managed Identity, RBAC.
- You need multiple named storage accounts in local or CI environments without manual hosts file edits.
- You want a single process to replace multiple emulators and the Docker Compose file that holds them together.
- You use Terraform with the `azurerm` provider and want a local target for `terraform apply`.
- You want the full Azure CLI (`az keyvault`, `az servicebus`, `az acr`, `az deployment`) to work locally, not just `az storage`.
- You want ARM-level resource management (resource groups, subscriptions, ARM templates, Bicep) in CI without a real subscription.
- You want `DefaultAzureCredential` to work end-to-end locally without code-path divergence from production.
- You want AI-assisted environment provisioning through MCP.

## Migrating

For Storage specifically, Topaz implements the same data-plane APIs Azurite does. Point your existing Azure SDK clients at Topaz's endpoints and they connect without code changes — only the endpoint hostname, port, and credentials change. The one item to check during migration is authentication: Topaz always enforces SharedKey signatures on Table and Queue requests, so any request that Azurite silently accepted with a missing or invalid signature will be rejected. This is intentional — it is the same behaviour real Azure has, and catching the divergence locally is the whole point.

Beyond Storage, the [API coverage docs](https://topaz.thecloudtheory.com/docs/api-coverage/) list which operations are implemented per service. If you hit something that is not yet supported, [open an issue](https://github.com/TheCloudTheory/Topaz/issues) — the backlog is publicly tracked and feedback shapes priorities.

## Update — v1.4 (19 May 2026)

The following gaps described in this post were addressed in the v1.4 release.

**SAS token validation.** Account SAS and Service SAS query strings (`?sv=…&sig=…`) are now validated on Blob, Queue, and Table data-plane endpoints. Stored access policies are honoured — revoking a named `<SignedIdentifier>` immediately blocks tokens referencing it via `si=`.

**Public-access Blob reads.** Containers created with `x-ms-blob-public-access: container` or `blob` now allow unauthenticated GET/HEAD requests. The public-access level is stored on the container and checked when no `Authorization` header is present.

**RA-GRS secondary endpoints (partial).** Secondary DNS hostnames (`{accountName}-secondary.*`), `secondaryEndpoints` in the ARM response, `GetServiceStats` on secondary endpoints, and `403 WriteOperationNotSupportedOnSecondary` on mutating secondary requests are all live. General data reads through secondary endpoints follow in v1.6.

**Table Storage OData queries.** `$filter`, `$select`, `$top`, and `$skiptoken` are now supported on Table entity query endpoints.

---

## Update — v1.5

**User Delegation SAS for Blob Storage.** The `POST /?restype=service&comp=userdelegationkey` data-plane endpoint is now implemented and requires a Bearer (Entra ID) token — SharedKey callers receive `403 AuthenticationFailed`, matching real Azure behaviour. The delegation key is derived deterministically from the storage account key and the caller's Entra OID and tenant ID via HMAC-SHA256, so no key material needs to be persisted. The data plane validates incoming User Delegation SAS tokens by re-deriving the same key at request time and comparing the signature, covering both per-blob (`sr=b`) and per-container (`sr=c`) resource scopes. Revocation of user delegation keys (`POST .../revokeUserDelegationKeys`) is tracked on the v1.8 roadmap.

---

## Summary

Azurite is a good Storage emulator. Topaz is a Storage emulator that also covers Key Vault, Service Bus, Event Hubs, Container Registry, Managed Identity, RBAC, ARM, and Entra ID — in one binary, with one log stream, one working directory, and one Docker image. The Storage parity is essentially complete; the only remaining Storage gap is general data reads through RA-GRS secondary endpoints, scheduled for v1.6.

If your application is Storage-only, stay on Azurite. If it is anything else — and most real Azure applications are — Topaz exists to remove the orchestration tax of running five different local emulators that were never designed to work together.

:::tip[Try it yourself]
Single binary. Runs locally. The Azure SDK, Azure CLI, Terraform, and `docker push` all work against it with no Azure subscription.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Getting started →](https://topaz.thecloudtheory.com/docs/intro) · Not ready to install? [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::
