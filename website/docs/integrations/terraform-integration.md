---
sidebar_position: 2
slug: /terraform-integration
description: Use Terraform with Topaz by configuring AzureRM to discover Topaz metadata endpoints, authenticate locally, and run apply/destroy against emulated Azure services.
keywords: [terraform topaz, azurerm local emulator, azure terraform local, topaz metadata_host, terraform azure emulator]
---

# Terraform integration

Topaz can be used as a local Azure target for Terraform by configuring Terraform providers to discover endpoints from Topaz metadata instead of Azure public cloud.

This page explains how Terraform integration works, how to configure it, and which settings are required.

## How it works

Terraform still uses the standard Azure providers, but endpoint discovery is redirected to Topaz:

1. Terraform providers read cloud metadata from Topaz ARM metadata endpoint.
2. Providers authenticate against Topaz Entra endpoints.
3. Resource management operations are sent to Topaz ARM/resource endpoints.
4. Resources are stored in Topaz local persistence.

In practice, this means your Terraform workflow (`init`, `plan`, `apply`, `destroy`) stays the same, but runs locally.

## Prerequisites

- Topaz installed and running
- DNS setup completed (see [Getting started](../intro.md))
- Certificate trusted by your runtime/tooling (or run in a containerized setup that already handles this)
- Terraform installed

## Start Topaz for Terraform

Use a deterministic subscription ID so your Terraform runs are repeatable:

```bash
topaz start \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

## Provider configuration

Topaz supports Terraform with both `azurerm` and `azapi` providers.

### AzureRM provider

Use this minimal provider configuration:

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

  # Important: host:port only (no scheme)
  metadata_host = "topaz.local.dev:8899"

  # Topaz does not emulate full RP registration flow.
  resource_provider_registrations = "none"
}
```

### AzAPI provider

The `azapi` provider requires its own endpoint configuration to reach Topaz instead of `management.azure.com`. Use the `endpoint` attribute (an HCL list) and set `disable_instance_discovery = true` so the Go provider does not try to contact Microsoft's login discovery service:

```hcl
terraform {
  required_providers {
    azapi = {
      source  = "azure/azapi"
      version = "~> 2.0"
    }
  }
}

variable "subscription_id" {
  type = string
}

provider "azapi" {
  subscription_id            = var.subscription_id
  use_msi                    = false
  use_oidc                   = false
  use_cli                    = true
  disable_instance_discovery = true

  endpoint = [{
    resource_manager_endpoint       = "https://topaz.local.dev:8899/"
    active_directory_authority_host = "https://topaz.local.dev:8899/"
    resource_manager_audience       = "https://topaz.local.dev:8899/"
  }]
}
```

If you use both providers together, combine them in one `terraform {}` block and configure each provider as shown above.

:::tip
Pass the subscription ID via an environment variable so it matches whatever Topaz was started with:

```bash
export TF_VAR_subscription_id=00000000-0000-0000-0000-000000000001
```
:::

### Why these fields matter

- `metadata_host`: tells AzureRM where to fetch cloud metadata.
- `resource_provider_registrations = "none"`: avoids AzureRM trying provider registration APIs that are not fully emulated.
- `endpoint` (azapi): overrides all three ARM endpoints (resource manager, authority host, and audience) so the provider never contacts Azure public cloud.
- `disable_instance_discovery` (azapi): prevents the Go-based provider from validating the authority URL against Microsoft's login discovery service, which is unreachable when pointing at a local emulator.

## Authentication configuration

For local development with Topaz, AzureRM authentication typically comes from the Azure CLI session.

Recommended flow:

1. Configure Azure CLI for Topaz cloud (see [Azure CLI integration](./azure-cli-integration.md)).
2. Login with Azure CLI.
3. Run Terraform in the same environment.

If needed, set explicit env vars so provider and tooling are deterministic:

```bash
export ARM_SUBSCRIPTION_ID=00000000-0000-0000-0000-000000000001
```

## Example resource

```hcl
resource "azurerm_resource_group" "example" {
  name     = "rg-local"
  location = "westeurope"
}
```

Then run:

```bash
terraform init
terraform apply -auto-approve
terraform destroy -auto-approve
```

## Configuration options and behavior

| Option | Provider | Required | Notes |
|---|---|---|---|
| `metadata_host` | azurerm | Yes | Host and port only — no scheme (e.g. `topaz.local.dev:8899`) |
| `resource_provider_registrations` | azurerm | Strongly recommended | Use `none` with Topaz |
| `endpoint` | azapi | Yes | List with `resource_manager_endpoint`, `active_directory_authority_host`, `resource_manager_audience` all pointing at `https://topaz.local.dev:8899/` |
| `disable_instance_discovery` | azapi | Yes | Set `true` to prevent the provider contacting Microsoft login discovery |
| `ARM_SUBSCRIPTION_ID` / `TF_VAR_subscription_id` | both | Recommended | Keep stable across runs |

## Common issues

| Symptom | Likely cause | Fix |
|---|---|---|
| `https://https://...` endpoint errors | `metadata_host` set with scheme | Use host:port only |
| `SubscriptionNotFound` | Subscription missing in emulator | Start Topaz with `--default-subscription` or create one first |
| azapi `endpoint` block syntax error | `endpoint` is an attribute list, not a block | Use `endpoint = [{ ... }]` syntax, not `endpoint { ... }` |
| azapi TLS / instance discovery failure | `disable_instance_discovery` not set | Add `disable_instance_discovery = true` to the `azapi` provider block |
| `CERTIFICATE_VERIFY_FAILED` / TLS errors | Certificate trust not configured in tool runtime | Follow cert setup in [Getting started](../intro.md) and [Azure CLI integration](./azure-cli-integration.md) |
