---
sidebar_position: 2
description: The difference between the Azure ARM control plane and service data planes — what each handles, why Topaz implements both, and why most Azure emulators only cover one.
keywords: [azure control plane data plane, arm control plane, azure data plane, topaz arm, terraform azure local, azure resource manager local]
---

# Control plane and data plane

Azure is split into two distinct API layers: the **ARM control plane** and service **data planes**. Understanding the distinction matters for local development because they serve different purposes, are addressed by different clients, and have historically been supported separately by emulators.

## The ARM control plane

The ARM (Azure Resource Manager) control plane lives at `management.azure.com`. It handles the *management* of Azure resources:

- Creating and deleting resource groups
- Provisioning storage accounts, Key Vaults, Service Bus namespaces
- Registering resources in a subscription
- Role assignments and policy
- ARM deployments (Bicep, ARM templates)

The control plane doesn't care what is *inside* a storage account. It only knows that a storage account named `mystorageaccount` exists in resource group `rg-dev`.

**Terraform uses the control plane exclusively.** When you run `terraform apply`, the `azurerm` provider calls `management.azure.com` to create or update each resource. It never calls the storage data plane to write a blob.

## Service data planes

Each Azure service exposes its own data plane — a separate API endpoint for working with the contents of a resource:

| Service | Data plane endpoint |
|---|---|
| Key Vault (secrets) | `https://{vault-name}.vault.azure.net` |
| Blob Storage | `https://{account}.blob.core.windows.net` |
| Service Bus (AMQP) | `amqps://{namespace}.servicebus.windows.net` |
| Event Hubs (AMQP) | `amqps://{namespace}.servicebus.windows.net` |
| Container Registry | `https://{registry}.azurecr.io` |

Azure SDKs target data planes directly. `SecretClient` talks to `vault.azure.net`, not `management.azure.com`. The SDK doesn't know or care how the vault was created — it only needs the vault's hostname to be reachable and to respond correctly to Key Vault REST protocol.

## Why both layers matter for local development

A local emulator that only implements the control plane can run Terraform plans but can't serve SDK requests. An emulator that only implements data planes can handle SDK traffic but can't be provisioned with Terraform.

Topaz implements both, which means a complete workflow is possible:

1. `terraform apply` provisions a Key Vault via the ARM control plane
2. Application code reads secrets from it via the Key Vault data plane
3. `terraform destroy` removes it via the ARM control plane

All three steps run locally, against the same Topaz instance, without a real Azure subscription.

## Why most emulators only do one

Implementing both layers is significantly more work. Azurite, for example, implements only the Azure Storage data planes. It has no concept of subscriptions, resource groups, or ARM deployments — because those are control plane concerns.

The Microsoft Service Bus Emulator implements the data plane (AMQP messaging) but does not expose an ARM control plane, so it cannot be provisioned by Terraform without extra workarounds.

Topaz is designed from the start to serve both layers for each supported service, which is what makes a full IaC-to-SDK development loop possible locally.

See [Supported services](../supported-services.md) for which operations are covered on each layer.
