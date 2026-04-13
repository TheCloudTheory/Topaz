---
sidebar_position: 2
description: Topaz is a local Azure emulator that runs Storage, Key Vault, Service Bus, Event Hubs, Container Registry, and more on your machine — no subscription, no internet connection, no cost.
keywords: [local azure emulator, local azure development, azure local emulator, azure emulator local, run azure locally, azure development without subscription]
---

# Local Azure emulator

Developing against real Azure is slow, expensive, and requires a subscription. Topaz is a local Azure emulator that runs the services your application depends on — Storage, Key Vault, Service Bus, Event Hubs, Container Registry, and more — directly on your machine, without connecting to the cloud.

## One process, every service

Most local Azure setups involve stitching together multiple tools: Azurite for storage, a third-party mock for Key Vault, the real Service Bus because nothing else exists. Each adds configuration, another process to manage, and another failure point in CI.

Topaz replaces the entire stack with a single binary:

```bash
topaz start --default-subscription 00000000-0000-0000-0000-000000000001
```

That one command gives you a running Azure environment — ARM control plane, storage, secrets, messaging, container registry — all on localhost.

## The same code, locally

Topaz implements the actual Azure REST APIs, not simplified mocks. The SDK calls, authentication flow, and error handling you write against Topaz work identically in production — the only difference is the endpoint URI, which points at the Topaz-specific domain instead of Azure:

```csharp
// Local: point at the Topaz-emulated vault
var client = new SecretClient(
    new Uri("https://myvault.keyvault.topaz.local.dev:8898"),
    new DefaultAzureCredential()
);
var secret = await client.GetSecretAsync("db-connection-string");
```

Everything else — credential resolution, retry behaviour, response parsing — behaves exactly as it would against a real vault. Swap the URI for `https://myvault.vault.azure.net` and the same code runs in production.

## Infrastructure as code, locally

Topaz includes a full ARM control plane. That means Terraform's `azurerm` provider can target Topaz with a one-line configuration change:

```hcl
provider "azurerm" {
  features {}
  metadata_host = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}
```

Your `terraform apply` creates real resources in Topaz — resource groups, storage accounts, Key Vaults, Service Bus namespaces — and `terraform destroy` cleans them up. No subscription. No cost. No drift between your local environment and what you declare in code.

## Built for CI

Topaz ships as a Docker image, making it straightforward to add to any CI pipeline as a service container:

```yaml
services:
  topaz:
    image: thecloudtheory/topaz-host:latest
    ports:
      - "8899:8899"
      - "8898:8898"
      - "8891:8891"
```

Tests run against a fresh Topaz instance on every build — no shared state, no leftover resources, no subscription required.

## What Topaz emulates

| Service | Coverage |
|---|---|
| Azure Storage (Blob, Table) | Partial — core operations |
| Azure Key Vault (secrets) | Full secret lifecycle |
| Azure Service Bus | Namespaces, queues, topics |
| Azure Event Hubs | Namespaces, event hubs |
| Azure Container Registry | Full Docker Registry V2 + OCI |
| Managed Identity | Token issuance |
| ARM control plane | Resource groups, subscriptions, deployments |
| Entra ID | Applications, service principals, groups |

See [Supported services](./supported-services.md) for the full operation-level breakdown.

## Next steps

- [Getting started](./intro.md) — install Topaz and run it for the first time
- [Terraform integration](./integrations/terraform-integration.md) — configure `azurerm` to target Topaz
- [Local Key Vault development](./tutorials/local-key-vault-development.md) — store and retrieve secrets locally
- [Azurite alternative](./comparisons/azurite-alternative.md) — detailed comparison with Azurite
