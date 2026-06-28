---
sidebar_position: 1
description: Looking for an Azurite alternative? Topaz emulates Azure Storage, Key Vault, Service Bus, Event Hubs, and more in a single binary — with full ARM, Terraform, and Azure CLI support.
keywords: [azurite alternative, azurite replacement, azure storage emulator alternative, local azure emulator, topaz vs azurite, azure emulator comparison]
---

# Azurite alternative

If you're using Azurite today and running into its limitations, Topaz is a drop-in alternative that covers Azure Storage — and goes significantly further.

## What is Azurite?

[Azurite](https://github.com/Azure/Azurite) is Microsoft's official local emulator for Azure Storage. It covers Blob, Queue, and Table storage and is widely used in CI pipelines and local development. It is a solid tool for storage-only workloads, but it is scoped to a single service and runs on Node.js.

Topaz is written in .NET 10 and ships as a single self-contained binary or Docker image. It emulates Azure Storage and the broader Azure platform — Key Vault, Service Bus, Event Hubs, Container Registry, Managed Identity, and more — all in one process.

## Storage feature comparison

For Azure Storage specifically, the two tools have different coverage and design choices. The table below reflects the current state of each emulator:

| Feature | Topaz | Azurite |
|---|---|---|
| **Endpoint URL format** | Hostname-based — real Azure style | IP + path by default; hostname optional |
| **Accounts** | Multiple named accounts via ARM | One fixed account; extras via env var + hosts file |
| **Connection strings** | Real Azure format | `UseDevelopmentStorage=true` shortcut |
| **Azure Storage Explorer** | Via connection string | Built-in emulator shortcut |
| **Runtime** | .NET 10, single binary or Docker | Node.js |
| **Blob: basic operations** (put, get, delete, head, list) | ✅ | ✅ |
| **Blob: metadata** (container get/set, blob get/set) | ✅ | ✅ |
| **Blob: container ACLs** | ✅ | ✅ |
| **Blob: container leases** | ✅ (acquire, renew, change, release, break) | ✅ |
| **Blob: blob leases** | ✅ (acquire, renew, change, release, break) | ✅ |
| **Blob: block blobs** (put-block, put-block-list, get-block-list) | ✅ | ✅ |
| **Blob: page blobs** (put-page, get-page-ranges) | ✅ | ✅ |
| **Blob: copy operations** | ✅ | ✅ |
| **Blob: snapshots** | ✅ | ✅ |
| **Blob: authentication** | Not enforced | Optional (enforced with `--oauth`) |
| **Blob / Table / Queue: SAS token validation** (Account SAS, Service SAS) | ✅ | ✅ |
| **Blob: User Delegation Key + User Delegation SAS** | ✅ | ✅ |
| **Blob / Table / Queue: stored access policy enforcement** | ✅ | ✅ |
| **Blob: public-access anonymous reads** | ✅ | ✅ |
| **Table: create, delete, query** | ✅ (stable) | ✅ (preview) |
| **Table: entities** (insert, upsert, merge, delete, query) | ✅ (stable) | ✅ (preview) |
| **Table: OData queries** ($filter, $select, $top, $skiptoken) | ✅ | ✅ |
| **Table: ACL** (stored access policies) | ✅ | ✅ |
| **Table: SharedKeyLite + SharedKey auth** | Always enforced | Optional |
| **Table: Entra ID / Bearer auth** | ✅ | Optional (with `--oauth`) |
| **Queue: create, delete, list** | ✅ | ✅ |
| **Queue: metadata** (get/set) | ✅ | ✅ |
| **Queue: ACL** (get/set stored access policies) | ✅ | ✅ |
| **Queue: messages** (enqueue, dequeue, peek, update, delete, clear) | ✅ | ✅ |
| **Queue: SharedKey auth** | Always enforced | Optional |
| **Queue: Entra ID / Bearer auth** | ✅ | Optional (with `--oauth`) |
| **RA-GRS (secondary endpoints)** | ✅ — DNS registration, `GetServiceStats`, read-only enforcement (v1.4); general data reads on secondary endpoints (v1.6) | ✅ |
| **Chaos engineering / fault injection** | ✅ — inject TransientError, Throttle, Timeout, ServiceUnavailable at configurable rates per service namespace | ❌ |

Topaz and Azurite are at near parity for Azure Storage data-plane operations. Features supported by Topaz include:

- **SAS token validation** — Account SAS and Service SAS signatures validated against the account key across all three services
- **Stored access policy enforcement** — policies resolved and applied for Blob containers, Queues, and Tables
- **User Delegation Key issuance** — `POST /?restype=service&comp=userdelegationkey` requires a Bearer token; the key is derived deterministically from the account key and the caller's Entra OID/TID, matching real Azure behaviour
- **User Delegation SAS validation** — validated for Blob Storage using the delegation key above; revocation via `POST .../revokeUserDelegationKeys` is enforced — SAS tokens whose `skt` predates the revocation timestamp are rejected
- **Public-access anonymous Blob reads** — containers configured with public access permit unauthenticated reads
- **RA-GRS secondary endpoints** — DNS registration, `GetServiceStats`, read-only enforcement, and general data reads through secondary endpoints for RA-GRS/RA-GZRS accounts
- **Full OData query support for Table** — `$filter`, `$select`, `$top`, `$skiptoken` on a stable, GA-quality implementation

Blob authentication is not fully enforced — unauthenticated requests to private containers are permitted. Azurite's Table Storage is still in preview.

## Endpoint and URL format

This is the most significant practical difference between the two tools.

**Azurite** uses an IP + path URL scheme by default:

```
http://127.0.0.1:10000/devstoreaccount1/mycontainer/myblob.txt
```

The account name sits in the URL *path* because `127.0.0.1` doesn't resolve subdomains. Azurite offers an optional production-style URL, but enabling it requires manually editing the hosts file and setting an `AZURITE_ACCOUNTS` environment variable before starting the emulator:

```
# hosts file
127.0.0.1 account1.blob.localhost
127.0.0.1 account1.queue.localhost
127.0.0.1 account1.table.localhost
```

```
set AZURITE_ACCOUNTS="account1:key1:key2"
```

**Topaz** uses the real Azure hostname-based scheme from the start:

```
https://myaccount.blob.storage.topaz.local.dev:8891/mycontainer/myblob.txt
```

When you create a storage account via the ARM API or CLI, Topaz automatically registers the DNS entry. No hosts file edits, no environment variables. The URL format is identical to a production Azure Storage URL, which means Azure SDK clients connect without any special emulator configuration beyond the endpoint hostname and port.

## Multiple storage accounts

Because Topaz has a full ARM control plane, you create as many named storage accounts as your application needs — just as in real Azure:

```bash
az storage account create --name sa-orders --resource-group rg-local --location westeurope
az storage account create --name sa-events --resource-group rg-local --location westeurope
```

Each account is fully isolated and gets its own connection string. In Azurite, adding accounts beyond the default `devstoreaccount1` requires setting the `AZURITE_ACCOUNTS` environment variable and manually adding entries to the hosts file for each additional account — then restarting the emulator. There is no API for creating or deleting accounts at runtime.

## Table Storage stability

Azurite's Table Storage is currently in **preview** and is not covered by Azure's general availability support terms. Topaz's Table implementation is stable, always enforces SharedKey and SharedKeyLite authentication, and additionally accepts Entra ID Bearer tokens — the same auth model used by the Azure SDK when connecting with a managed identity or service principal.

## Authentication model

Both emulators support SharedKey authentication. The difference is in how strictly it is applied and what additional schemes are supported:

| | Topaz | Azurite |
|---|---|---|
| Blob auth enforcement | Not enforced | Optional (`--oauth` flag) |
| Table auth enforcement | Always enforced | Optional |
| Queue auth enforcement | Always enforced | Optional |
| Entra ID Bearer tokens (Table, Queue) | ✅ | Optional (`--oauth` flag) |
| SAS token validation (Blob, Table, Queue) | ✅ | ✅ |
| Stored access policy enforcement | ✅ | ✅ |
| Public-access anonymous Blob reads | ✅ | ✅ |

Topaz always validates Table and Queue request signatures. If an application sends an incorrectly signed request that Azurite silently accepts in default mode, it will fail against Topaz. This makes Topaz stricter by default — which catches auth bugs earlier.

As of v1.4, SAS token validation is enforced across all three services — Account SAS and Service SAS query strings are validated against the account key, stored access policies can be revoked, and containers configured with public access correctly permit unauthenticated reads. User Delegation SAS is also validated for Blob Storage: the delegation key is derived from the account key and the caller's Entra OID/TID, so no key persistence is required. `POST .../revokeUserDelegationKeys` persists a per-account revocation timestamp; any User Delegation SAS whose `skt` predates it is rejected.

## Azure Storage Explorer

Topaz supports [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/). Connect using a storage account connection string, which Topaz provides in the same format as a real Azure connection string — no special emulator mode is needed.

Retrieve the connection string for an account:

```bash
topaz storage account show-connection-string --name sa-orders
```

Then paste it into Storage Explorer under **Connect to Azure Storage → Connection string**. Azurite has a dedicated emulator shortcut in Storage Explorer; Topaz uses the standard connection string path because each account is a first-class named resource.

## Beyond storage

Azurite is scoped entirely to Azure Storage. Topaz emulates the broader Azure platform in a single process:

| Service | Topaz | Azurite |
|---|---|---|
| Azure Storage (Blob, Table, Queue) | ✅ | ✅ |
| Key Vault (secrets, keys, certificates) | ✅ | ❌ |
| Service Bus (AMQP + HTTPS) | ✅ | ❌ |
| Event Hubs (AMQP + HTTPS) | ✅ | ❌ |
| Container Registry (push, pull, tags) | ✅ | ❌ |
| Managed Identity | ✅ | ❌ |
| Entra ID (local token issuance) | ✅ | ❌ |
| RBAC (role assignments) | ✅ | ❌ |
| ARM control plane (resource groups, subscriptions) | ✅ | ❌ |
| ARM template / Bicep deployments | ✅ | ❌ |
| Terraform `azurerm` provider target | ✅ | ❌ |
| Azure CLI (`az keyvault`, `az servicebus`, …) | ✅ | ❌ |
| MCP server for AI tooling | ✅ | ❌ |

## When to keep Azurite

Azurite is the right choice if:

- Your application uses only Azure Storage and a single account is sufficient
- You need a mature, Microsoft-maintained emulator with high compatibility guarantees
- Your toolchain is already built around Azurite and migration is not worth the effort

## When to switch to Topaz

Topaz is the right choice if:

- Your application uses any service beyond Storage — Key Vault, Service Bus, Event Hubs, Container Registry, or Managed Identity
- You need multiple named storage accounts in local or CI environments without manual hosts file edits
- You want a single process to replace multiple emulators
- You use Terraform with the `azurerm` provider and need a local target for `terraform apply`
- You want the full Azure CLI (`az keyvault`, `az servicebus`, etc.) to work locally, not just `az storage`
- You want ARM-level resource management (resource groups, subscriptions) in CI without a real subscription
- You want Entra ID Bearer token authentication enforced on Table and Queue storage
- You want Table Storage on a stable, GA-quality implementation

## Migrating from Azurite

Topaz implements the same Azure Storage data-plane APIs that Azurite does. For Blob, Queue, and Table storage, you can point your existing Azure SDK clients at Topaz's endpoints without code changes — the only differences are the endpoint hostname, port, and credentials. See [Getting started](../intro.md) for installation and DNS setup.

The one area to check during migration is authentication. Topaz always enforces Table and Queue signatures, so any request that was silently accepted by Azurite without a valid SharedKey signature will be rejected. Update connection strings and storage account keys before testing.

For services beyond storage, Topaz adds emulation that has no Azurite equivalent. The [integrations](../integrations/azure-cli-integration.md) section covers how to wire up the Azure CLI and Terraform.
