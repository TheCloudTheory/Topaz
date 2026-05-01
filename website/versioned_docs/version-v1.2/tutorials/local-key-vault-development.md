---
sidebar_position: 3
description: Use Topaz for Azure Key Vault local development — create a vault, store and retrieve secrets, and connect the Azure SDK and Azure CLI without a real Azure subscription.
keywords: [azure key vault local, key vault local development, local key vault emulator, topaz key vault, azure key vault emulator, key vault secrets local]
---

# Local Key Vault development with Topaz

This tutorial walks through a complete Azure Key Vault local development workflow using Topaz: create a vault, store secrets, retrieve them with the Azure CLI and the Azure SDK, and integrate with a .NET application — all without connecting to real Azure.

## What you will build

- A local Key Vault instance running on Topaz
- Secrets stored and retrieved via the Azure CLI
- A .NET snippet connecting to the local vault using `SecretClient`

## Prerequisites

- Topaz installed and running (see [Getting started](../intro.md))
- DNS setup completed
- Topaz certificate trusted by your OS and tooling
- Azure CLI installed (`az --version`)
- Topaz cloud registered in Azure CLI (see [Azure CLI integration](../integrations/azure-cli-integration.md))

## Step 1: Start Topaz

```bash
topaz start \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

## Step 2: Set the active cloud to Topaz

```bash
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001
```

## Step 3: Create a resource group and Key Vault

```bash
az group create \
  --name rg-local \
  --location westeurope

az keyvault create \
  --name myvault \
  --resource-group rg-local \
  --location westeurope
```

Topaz assigns the vault a local hostname: `myvault.vault.topaz.local.dev`, which resolves to `127.0.0.1` via the DNS setup you completed in the prerequisites.

## Step 4: Store and retrieve secrets

Set a secret:

```bash
az keyvault secret set \
  --vault-name myvault \
  --name MySecret \
  --value "hello-from-topaz"
```

Retrieve it:

```bash
az keyvault secret show \
  --vault-name myvault \
  --name MySecret \
  --query value \
  --output tsv
```

Expected output: `hello-from-topaz`

List all secrets in the vault:

```bash
az keyvault secret list --vault-name myvault --output table
```

## Step 5: Connect with the Azure SDK (.NET)

Install the Azure Key Vault secrets client:

```bash
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
```

Connect to the local vault using `DefaultAzureCredential`, which picks up the Azure CLI session automatically:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var vaultUri = new Uri("https://myvault.vault.topaz.local.dev:8898");
var client = new SecretClient(vaultUri, new DefaultAzureCredential());

KeyVaultSecret secret = await client.GetSecretAsync("MySecret");
Console.WriteLine(secret.Value); // hello-from-topaz
```

:::tip[Switching to production]

The only difference from production is the URI. Replace `https://myvault.vault.topaz.local.dev:8898` with `https://myvault.vault.azure.net` and the rest of the code — credentials, SDK calls, response handling — is identical.

:::

## Step 6: Soft-delete and recovery

Topaz emulates soft-delete behaviour. Delete a secret:

```bash
az keyvault secret delete --vault-name myvault --name MySecret
```

List deleted secrets:

```bash
az keyvault secret list-deleted --vault-name myvault --output table
```

Recover a deleted secret:

```bash
az keyvault secret recover --vault-name myvault --name MySecret
```

Purge (permanent delete):

```bash
az keyvault secret purge --vault-name myvault --name MySecret
```

## API coverage

Topaz implements the full secrets lifecycle. See the [Key Vault API coverage](../api-coverage/key-vault.md) page for the complete operation matrix. Keys and Certificates are not yet emulated.

## Next steps

- [Terraform integration](../integrations/terraform-integration.md) — provision a Key Vault with Terraform locally using `azurerm_key_vault` and `azurerm_key_vault_secret`
- [Supported services](../supported-services.md) — full service coverage matrix
