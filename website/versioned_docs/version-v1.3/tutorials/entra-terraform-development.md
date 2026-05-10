---
sidebar_position: 2
description: Manage Azure Active Directory (Entra ID) objects — users, groups, applications, and service principals — with Terraform and the azuread provider against a local Topaz emulator.
keywords: [topaz entra, azuread terraform, terraform azure ad local, topaz azuread provider, service principal terraform local]
---

# Managing Entra ID objects with Terraform and Topaz

This tutorial walks through managing Entra ID (Azure Active Directory) resources locally using Terraform's `azuread` provider with Topaz as the identity backend.

You will learn:

- How to configure the `azuread` provider to point at Topaz
- How to create and destroy users, groups, applications, and service principals
- How to combine `azurerm` and `azuread` in a single Terraform project
- Common gotchas specific to Entra ID emulation

## What you will build

By the end of this tutorial, Terraform will create and then destroy:

- An Entra application registration
- A service principal linked to that application
- A security group
- A user account

All operations run locally against Topaz — no real Azure or Entra resources are created.

## Prerequisites

- Topaz installed and running
- DNS setup completed (see [Getting started](../intro.md))
- Topaz certificate trusted by your OS and tooling (see [Getting started](../intro.md))
- Terraform installed (`terraform --version`)
- Azure CLI installed and configured for Topaz (see [Azure CLI integration](../integrations/azure-cli-integration.md))

## Step 1: Start Topaz

Start Topaz with a stable tenant and subscription ID:

```bash
topaz-host start \
  --tenant-id 50717675-3E5E-4A1E-8CB5-C62D8BE8CA48 \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

Keep Topaz running for the rest of this tutorial.

## Step 2: Create a Terraform project

Create a working directory:

```bash
mkdir -p topaz-entra-tutorial
cd topaz-entra-tutorial
```

Create `providers.tf`:

```hcl
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
  }
}

provider "azuread" {
  # Host and port only — do not include https://
  metadata_host = "topaz.local.dev:8899"
}
```

The `azuread` provider redirects all Microsoft Graph API calls to Topaz as soon as `metadata_host` is set. No other endpoint overrides are needed.

## Step 3: Create Entra resources

Create `main.tf` with an application, service principal, group, and user:

```hcl
# Application registration
resource "azuread_application" "app" {
  display_name = "my-local-app"
}

# Service principal backed by the application above
resource "azuread_service_principal" "sp" {
  client_id = azuread_application.app.client_id
}

# Security group
resource "azuread_group" "devs" {
  display_name     = "local-developers"
  security_enabled = true
}

# User account
resource "azuread_user" "alice" {
  user_principal_name   = "alice@mytenant.onmicrosoft.com"
  display_name          = "Alice"
  password              = "P@ssw0rd!"
  force_password_change = false
}

output "app_client_id" {
  value = azuread_application.app.client_id
}

output "sp_object_id" {
  value = azuread_service_principal.sp.object_id
}

output "group_object_id" {
  value = azuread_group.devs.object_id
}

output "user_upn" {
  value = azuread_user.alice.user_principal_name
}
```

## Step 4: Run the Terraform workflow

Initialize:

```bash
terraform init
```

Plan:

```bash
terraform plan -out tfplan
```

Apply:

```bash
terraform apply -auto-approve tfplan
```

Terraform should report four resources created and print the output values.

## Step 5: Verify with Azure CLI

Confirm the objects exist in Topaz:

```bash
# List applications
az ad app list --display-name my-local-app --output table

# List service principals
az ad sp list --display-name my-local-app --output table

# List groups
az ad group list --display-name local-developers --output table

# Show user
az ad user show --id alice@mytenant.onmicrosoft.com --output table
```

## Step 6: Clean up

```bash
terraform destroy -auto-approve
```

Terraform will remove the service principal before the application because of the dependency declared through `client_id`.

## Combining azuread with azurerm

If you need both Entra and ARM resources in the same project, add `azurerm` to the `required_providers` block and configure both providers:

```hcl
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
  metadata_host                   = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}

provider "azuread" {
  metadata_host = "topaz.local.dev:8899"
}
```

Both providers use the same `metadata_host` value and authenticate via the same Azure CLI session.

## Common gotchas

### 1) `metadata_host` includes a scheme

Symptom: Terraform errors with a malformed endpoint such as `https://https://...`

Fix: Use `metadata_host = "topaz.local.dev:8899"` — host and port only, no `https://`.

### 2) `unexpected number of service principals returned`

Symptom:

```
Error: Could not list existing service principals
unexpected number of service principals returned (expected: 1, received: N)
```

Cause: The `azuread` provider looks up a service principal by `appId` using a `$filter` query when reading existing state. If Topaz has leftover service principals from a previous run, it may return multiple results.

Fix: Run `terraform destroy` to clean up the previous run, or clear the Topaz state directory (`.topaz/`), then re-apply.

### 3) `AZURE_CORE_INSTANCE_DISCOVERY` not set

Symptom: Azure CLI authentication fails during `terraform apply` because the CLI tries to contact `login.microsoftonline.com`.

Fix:

```bash
export AZURE_CORE_INSTANCE_DISCOVERY=false
```

Set this before running any Terraform command when using Topaz as the auth endpoint.

### 4) TLS certificate errors

Symptom: `CERTIFICATE_VERIFY_FAILED` from the CLI or Terraform provider.

Fix: Ensure the Topaz certificate is trusted. See [Getting started](../intro.md) and [Azure CLI integration](../integrations/azure-cli-integration.md).

### 5) User principal name domain mismatch

Symptom: Terraform applies successfully but Azure CLI queries return no results for a UPN.

Cause: The UPN domain (`@mytenant.onmicrosoft.com`) must match the tenant configured in Topaz (or simply be any consistent domain — Topaz does not validate it against real DNS).

Fix: Use a consistent domain suffix across all users in the same project. The domain itself does not need to exist.

## Next steps

- Assign users to groups using `azuread_group_member`
- Combine Entra objects with Key Vault access policies in a single Terraform project
- See [Terraform integration](../integrations/terraform-integration.md) for full provider reference
