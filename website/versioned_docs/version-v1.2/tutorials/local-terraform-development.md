---
sidebar_position: 1
description: Step-by-step tutorial for Terraform AzureRM local testing with Topaz. Covers provider configuration, authentication, init/plan/apply/destroy, and common gotchas — no Azure subscription needed.
keywords: [topaz tutorial, terraform local azure, azurerm with topaz, local terraform development, topaz gotchas, terraform azurerm local testing, terraform local testing azure]
---

# Local Terraform development with Topaz

This tutorial walks through a full local Terraform workflow using Topaz as your Azure target.

You will learn:

- Initial machine and tool configuration
- A working Terraform project layout
- Exact provider configuration required for Topaz
- Step-by-step `init` → `plan` → `apply` → `destroy`
- Common gotchas and how to fix them quickly

## What you will build

In this tutorial, Terraform will create:

- A resource group
- A storage account

Everything runs locally against Topaz. No real Azure resources are created.

## Prerequisites

Before you start:

- Topaz installed
- DNS setup completed (see [Getting started](../intro.md))
- Topaz certificate trusted by your OS and tooling (see [Getting started](../intro.md))
- Terraform installed (`terraform --version`)
- Azure CLI installed (`az --version`)

## Step 1: Start Topaz with deterministic IDs

Start Topaz with a stable subscription ID and your Entra tenant ID:

```bash
topaz start \
  --tenant-id <your-entra-tenant-id> \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

Why this matters:

- `--default-subscription` makes Terraform runs repeatable.
- `--tenant-id` is needed for local auth flows that rely on Entra metadata.

Keep Topaz running for the rest of this tutorial.

## Step 2: Configure Azure CLI for Topaz cloud

If you have not done this before, follow [Azure CLI integration](../integrations/azure-cli-integration.md).

Minimal flow:

```bash
# One-time registration
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/refs/heads/main/cloud.json -o cloud.json
az cloud register -n Topaz --cloud-config @"cloud.json"

# Use Topaz cloud
az cloud set -n Topaz

# Required for Topaz Entra endpoint
export AZURE_CORE_INSTANCE_DISCOVERY=false

# Login
az login
```

Verify:

```bash
az account show --output table
```

## Step 3: Create a Terraform project

Create a clean working directory:

```bash
mkdir -p topaz-terraform-tutorial
cd topaz-terraform-tutorial
```

Create `providers.tf` for the **azurerm** provider:

```hcl
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
  }
}

provider "azurerm" {
  features {}

  # Important: host:port only. Do not prefix with https://
  metadata_host = "topaz.local.dev:8899"

  # Topaz does not emulate full RP registration flow.
  resource_provider_registrations = "none"
}
```

If you want to use the **azapi** provider instead (or alongside azurerm), the configuration is different — `azapi` does not use `metadata_host`. Instead, use the `endpoint` list attribute and set `disable_instance_discovery = true`:

```hcl
terraform {
  required_version = ">= 1.6.0"

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

Pass the subscription ID via an environment variable:

```bash
export TF_VAR_subscription_id=00000000-0000-0000-0000-000000000001
```

Create `main.tf`:

```hcl
resource "azurerm_resource_group" "rg" {
  name     = "rg-topaz-tutorial"
  location = "westeurope"
}

resource "azurerm_storage_account" "sa" {
  name                     = "topaztutorialsa001"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}
```

Set a deterministic subscription in your shell:

```bash
export ARM_SUBSCRIPTION_ID=00000000-0000-0000-0000-000000000001
```

## Step 4: Run Terraform workflow

Initialize:

```bash
terraform init
```

Create a plan:

```bash
terraform plan -out tfplan
```

Apply:

```bash
terraform apply -auto-approve tfplan
```

You should see resources created successfully.

## Step 5: Verify with Azure CLI

Confirm resources exist in Topaz:

```bash
az group show --name rg-topaz-tutorial --output table
az storage account show --name topaztutorialsa001 --resource-group rg-topaz-tutorial --output table
```

## Step 6: Clean up

Destroy resources:

```bash
terraform destroy -auto-approve
```

## Common gotchas

### 1) `metadata_host` includes scheme

Symptom:

- Terraform errors with malformed endpoint such as `https://https://...`

Fix:

- Use `metadata_host = "topaz.local.dev:8899"`
- Do not include `https://`

### 2) `SubscriptionNotFound`

Symptom:

- `terraform apply` fails because the subscription does not exist

Fix:

- Start Topaz with `--default-subscription 00000000-0000-0000-0000-000000000001`
- Export the same ID in `ARM_SUBSCRIPTION_ID`

### 3) TLS certificate errors

Symptom:

- `CERTIFICATE_VERIFY_FAILED` from `az` or provider auth flow

Fix:

- Ensure `topaz.crt` is trusted in your OS trust store
- Ensure Azure CLI certificate bundle is updated (see [Azure CLI integration](../integrations/azure-cli-integration.md))

### 4) Wrong active cloud in Azure CLI

Symptom:

- `az` commands hit real Azure instead of Topaz

Fix:

- Check cloud: `az cloud show --output table`
- Switch if needed: `az cloud set -n Topaz`

### 5) Provider registration errors

Symptom:

- AzureRM tries RP registration calls unsupported by local emulator behavior

Fix:

- Keep `resource_provider_registrations = "none"`

### 6) azapi `endpoint` block syntax error

Symptom:

- Terraform errors with `Blocks of type "endpoint" are not expected here`

Fix:

- `endpoint` in the `azapi` provider is an attribute (list), not a block — use `endpoint = [{ ... }]` syntax, not `endpoint { ... }`

### 7) azapi TLS or instance discovery failure

Symptom:

- azapi provider fails during `init` or `apply` trying to reach `login.microsoftonline.com`

Fix:

- Add `disable_instance_discovery = true` to your `azapi` provider block
- Ensure `SSL_CERT_FILE` (or your OS trust store) includes the Topaz certificate so the Go-based provider trusts TLS connections to `topaz.local.dev`

## Next steps

- Extend `main.tf` with Key Vault or Service Bus resources
- Add this workflow to CI using a local Topaz container
- See [Terraform integration](../integrations/terraform-integration.md) for reference configuration details
