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

If you also use AzAPI resources, keep `azurerm` configured for Topaz metadata and add `azapi`:

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
  metadata_host = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}

provider "azapi" {}
```

### Why these fields matter

- `metadata_host`: tells AzureRM where to fetch cloud metadata.
- `resource_provider_registrations = "none"`: avoids AzureRM trying provider registration APIs that are not fully emulated.

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

| Option | Required | Default | Notes |
|---|---|---|---|
| `metadata_host` | Yes | none | Must be `topaz.local.dev:8899`-style host and port, not `https://...` |
| `resource_provider_registrations` | Strongly recommended | provider default | Use `none` with Topaz |
| `ARM_SUBSCRIPTION_ID` | Recommended | from auth context | Keep stable across runs |

## Common issues

| Symptom | Likely cause | Fix |
|---|---|---|
| `https://https://...` endpoint errors | `metadata_host` set with scheme | Use host:port only |
| `SubscriptionNotFound` | Subscription missing in emulator | Start Topaz with `--default-subscription` or create one first |
| `CERTIFICATE_VERIFY_FAILED` / TLS errors | Certificate trust not configured in tool runtime | Follow cert setup in [Getting started](../intro.md) and [Azure CLI integration](./azure-cli-integration.md) |
