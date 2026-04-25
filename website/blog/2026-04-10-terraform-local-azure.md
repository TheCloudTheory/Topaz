---
slug: terraform-local-azure-no-subscription
title: "Running Terraform against Azure locally, without a subscription"
authors: kamilmrzyglod
tags: [general, terraform]
---

Every Terraform workflow that targets Azure needs the same things before it can do anything useful: an Azure subscription, a service principal or user account with the right permissions, and a network path to the Azure APIs. In a team setting you also need to make sure those credentials are available wherever `terraform apply` runs — local machines, CI agents, staging pipelines. The feedback loop is slow, and the blast radius for a misconfigured `apply` is real.

Topaz removes all of that. The same `terraform apply` that would create resources in Azure can instead create them in a local emulator, with no subscription, no credentials to rotate, and no cloud charges. This post explains how the integration works and how to set it up.

{/* truncate */}

## Why the standard AzureRM provider works at all

The key insight is that the AzureRM provider does not have Azure's API endpoints hardcoded. When it initialises, it fetches a metadata document from a discovery endpoint that describes where each Azure API lives. In a normal setup, that discovery endpoint is the Azure Resource Manager metadata endpoint at `management.azure.com`. Once the provider has that document, it constructs every subsequent request URL from it.

The `metadata_host` setting exists precisely to point that discovery step somewhere else. Set it to Topaz's ARM port, and the provider fetches Topaz's metadata document instead. That document points every API URL — authentication, resource management, Key Vault, Storage — at the local emulator. The provider never knows it is not talking to Azure.

```hcl
provider "azurerm" {
  features {}
  metadata_host                    = "topaz.local.dev:8899"
  resource_provider_registrations  = "none"
}
```

Two settings are doing the work here. `metadata_host` redirects endpoint discovery to Topaz. `resource_provider_registrations = "none"` tells the provider not to attempt registering resource providers on startup — Topaz does not emulate the full registration flow, and it is not needed for local development anyway.

## DNS setup

The hostname `topaz.local.dev` needs to resolve to `127.0.0.1` on your machine. Topaz ships install scripts that configure this using `dnsmasq`, which handles the wildcard subdomains that services like Container Registry depend on. The [getting started guide](https://topaz.thecloudtheory.com/docs/intro) covers installation and the one-time DNS and certificate setup.

For containerised environments, the same configuration can be handled at the container network level — the [Terraform integration guide](https://topaz.thecloudtheory.com/docs/terraform-integration) covers that path as well.

## Authentication

AzureRM needs a credential to authenticate against whatever it thinks Azure is. With Topaz, that means authenticating against Topaz's local Entra ID emulation layer, which responds to the same OAuth2 endpoints the real Azure uses.

The simplest approach for local development is to use the Azure CLI configured for Topaz's local cloud. Once configured, `az login` issues a token from the local Entra layer, and the AzureRM provider picks it up through `DefaultAzureCredential` exactly as it would in production. No service principal, no environment variables, no secrets.

A fixed subscription ID avoids drift between runs:

```bash
topaz start \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

Pair that with:

```bash
export ARM_SUBSCRIPTION_ID=00000000-0000-0000-0000-000000000001
```

And every `terraform plan` and `apply` targets the same subscription, the same resource IDs, and the same state — which matters when you want your CI runs to behave identically to your laptop.

## A complete example

With Topaz running, this is all it takes to create a resource group and a Key Vault locally:

```hcl
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
  }
}

provider "azurerm" {
  features {}
  metadata_host                   = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}

resource "azurerm_resource_group" "example" {
  name     = "rg-local"
  location = "westeurope"
}

resource "azurerm_key_vault" "example" {
  name                = "kv-local"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  tenant_id           = "50717675-3e5e-4a1e-8cb5-c62d8be8ca48"
  sku_name            = "standard"
}
```

```bash
terraform init
terraform apply -auto-approve
terraform destroy -auto-approve
```

The tenant ID above is Topaz's built-in local tenant — the same one `az login` uses when configured for the local cloud.

## AzAPI provider

If your Terraform configuration uses [`azapi`](https://registry.terraform.io/providers/Azure/azapi) resources alongside `azurerm`, the setup is straightforward. Keep the `azurerm` provider configured for Topaz metadata and add the `azapi` provider declaration:

```hcl
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
    azapi = {
      source = "Azure/azapi"
    }
  }
}

provider "azurerm" {
  features {}
  metadata_host                   = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}

provider "azapi" {}
```

Because `azapi` inherits its endpoint configuration from the same environment, and Topaz's metadata document covers the resource management endpoints that `azapi` targets, no additional configuration is needed.

## Using it in CI

The setup above works identically in a CI pipeline. Run Topaz as a service container or a background step, set `ARM_SUBSCRIPTION_ID`, and run `terraform apply` as normal. No Azure credentials in the pipeline, no cost per run, no rate limiting from the Azure APIs. The [CI/CD integration guide](https://topaz.thecloudtheory.com/docs/ecosystem/ci-cd) has ready-to-use examples for GitHub Actions and Azure DevOps.

## What works today

Topaz currently supports Terraform workflows for Azure Storage, Key Vault, Service Bus, Event Hubs, Container Registry, and Resource Manager operations including resource groups and ARM template deployments. The [API coverage docs](https://topaz.thecloudtheory.com/docs/api-coverage/) list which operations are implemented per service.

Not every AzureRM resource type is emulated yet. If you hit a resource that Topaz does not support, the provider will return a `404` or an unsupported operation error. Check the API coverage page for current status, and open an issue if something you need is missing.

:::tip[Try it in 5 minutes]
Topaz installs as a single binary with no Azure subscription required. The getting-started guide walks through installation, DNS setup, and your first `terraform apply` against the local emulator.

[Get started →](/docs/intro)
:::
