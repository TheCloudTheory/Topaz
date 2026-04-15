---
sidebar_position: 1
description: Looking for an Azurite alternative? Topaz emulates Azure Storage, Key Vault, Service Bus, Event Hubs, and more in a single binary — with full ARM, Terraform, and Azure CLI support.
keywords: [azurite alternative, azurite replacement, azure storage emulator alternative, local azure emulator, topaz vs azurite, azure emulator comparison]
---

# Azurite alternative

If you're using Azurite today and running into its limitations, Topaz is a drop-in alternative that covers Azure Storage — and goes significantly further.

## What is Azurite?

[Azurite](https://github.com/Azure/Azurite) is Microsoft's official local emulator for Azure Storage. It covers Blob, Queue, and Table storage and is widely used in CI pipelines and local development. It is a solid tool for storage-only workloads, but it is scoped to a single service.

## Storage feature comparison

For Azure Storage specifically, the two tools have different coverage and different design choices. The table below reflects the current state of each emulator:

| Feature | Topaz | Azurite |
|---|---|---|
| **Accounts** | Multiple named accounts via ARM | One fixed emulated account |
| **Connection strings** | Real Azure format | `UseDevelopmentStorage=true` shortcut |
| **Azure Storage Explorer** | Via connection string | Built-in emulator shortcut |
| **Blob: basic operations** (put, get, delete, head, list) | ✅ | ✅ |
| **Blob: metadata** (container get/set, blob set) | ✅ Container; set-only for blobs | ✅ |
| **Blob: container ACLs** | ✅ | ✅ |
| **Blob: container leases** | ✅ (acquire, renew, change, release, break) | ✅ |
| **Blob: blob leases** | <span class="badge--coming-soon">Coming soon</span> | ✅ |
| **Blob: block blobs** (put-block, put-block-list) | <span class="badge--coming-soon">Coming soon</span> | ✅ |
| **Blob: page blobs** | <span class="badge--coming-soon">Coming soon</span> | ✅ |
| **Blob: copy operations** | <span class="badge--coming-soon">Coming soon</span> | ✅ |
| **Blob: snapshots** | <span class="badge--coming-soon">Coming soon</span> | ✅ |
| **Blob: authentication** | Not enforced | Optional (enforced with `--oauth`) |
| **Table: create, delete, query** | ✅ | ✅ |
| **Table: entities** (insert, upsert, merge, delete, query) | ✅ | ✅ |
| **Table: SharedKeyLite + SharedKey auth** | Always enforced | Optional |
| **Queue Storage** | <span class="badge--coming-soon">Coming soon</span> | ✅ |

The trade-off is scope: Azurite is more complete for Storage alone; Topaz covers a narrower Storage feature set in exchange for emulating the wider Azure service landscape in the same process.

## Multiple storage accounts

Because Topaz has a full ARM control plane, you can create as many named storage accounts as your application needs — just as you would in real Azure:

```bash
az storage account create --name sa-orders --resource-group rg-local --location westeurope
az storage account create --name sa-events --resource-group rg-local --location westeurope
```

Each account gets its own connection string and is fully isolated. Azurite, by contrast, runs as a single fixed account — you cannot create additional accounts or address them independently.

## Azure Storage Explorer

Topaz supports [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/). Connect using a storage account connection string, which Topaz provides in the same format as a real Azure connection string — no `UseDevelopmentStorage=true` shortcut or special emulator mode is needed.

Retrieve the connection string for an account:

```bash
topaz storage account show-connection-string --name sa-orders
```

Then paste it into Storage Explorer under **Connect to Azure Storage → Connection string**. Azurite has a dedicated emulator shortcut in Storage Explorer; Topaz uses the standard connection string path because each account is a first-class named resource.

## When to keep Azurite

Azurite is the right choice if:

- Your application uses only Azure Storage and a single account is sufficient
- You need Queue Storage, block blobs, page blobs, copy operations, blob snapshots, or blob-level leases — Topaz does not implement these yet
- You need a mature, Microsoft-maintained emulator with high compatibility guarantees
- Your toolchain is already built around Azurite and migration is not worth the effort

## When to switch to Topaz

Topaz is the right choice if:

- Your application uses Key Vault, Service Bus, Event Hubs, or any service beyond Storage
- You need multiple named storage accounts in local or CI environments
- You want a single process to replace multiple emulators
- You use Terraform with the `azurerm` provider and need a local target for `terraform apply`
- You want the full Azure CLI (`az keyvault`, `az servicebus`, etc.) to work locally, not just `az storage`
- You want ARM-level resource management (resource groups, subscriptions) in CI without a real subscription

## Migrating from Azurite

Topaz implements the same Azure Storage data-plane APIs that Azurite does. For Blob, Queue, and Table storage, you can point your existing Azure SDK connection strings at Topaz's storage port without code changes. See [Getting started](../intro.md) for installation and DNS setup.

For services beyond storage, Topaz adds emulation that has no Azurite equivalent. The [integrations](../integrations/azure-cli-integration.md) section covers how to wire up the Azure CLI and Terraform.
