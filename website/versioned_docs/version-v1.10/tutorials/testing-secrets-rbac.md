---
sidebar_position: 11
description: Test Azure Key Vault secrets and RBAC role assignments with Topaz — validate least-privilege access before deploying to real Azure. Includes positive and negative test cases.
keywords: [azure rbac testing, key vault secrets testing, managed identity testing, topaz rbac, azure least privilege test, key vault emulator rbac, azure role assignment test]
---

# Testing secrets and RBAC pipelines with Topaz

In this tutorial, we will test a complete secrets-access pipeline: create a Key Vault, store a secret, assign a Managed Identity with the *Key Vault Secrets User* role, and verify the identity can read the secret. We will also write a negative test that confirms an unassigned identity is denied access.

This pattern is valuable for validating least-privilege IAM configurations before they are deployed to a real Azure environment.

A complete runnable example is available in [`Examples/Topaz.Example.SecretsRbac`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.SecretsRbac).

## What you will build

- A Key Vault and secret created via the Azure CLI
- A Managed Identity with a scoped RBAC role assignment
- A .NET snippet that reads the secret using `DefaultAzureCredential`
- A negative test confirming access is denied without the role

## Prerequisites

- Topaz installed and running (see [Getting started](../intro.md))
- DNS setup completed and Topaz certificate trusted
- Azure CLI installed (`az --version`)
- Topaz cloud registered in Azure CLI (see [Azure CLI integration](../integrations/azure-cli-integration.md))
- .NET 10 SDK installed

:::note[Before you start]
Topaz must be running and the Azure CLI pointed at it. See [Getting started](../intro.md) and [Azure CLI integration](../integrations/azure-cli-integration.md), then activate:

```bash
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001
```
:::

## Step 1: Provision the infrastructure

Create a resource group, Key Vault, and Managed Identity:

```bash
az group create \
  --name rg-rbac-test \
  --location westeurope

az keyvault create \
  --name kv-rbac-test \
  --resource-group rg-rbac-test \
  --location westeurope

az keyvault secret set \
  --vault-name kv-rbac-test \
  --name DatabasePassword \
  --value "super-secret-value"

az identity create \
  --name id-app \
  --resource-group rg-rbac-test \
  --location westeurope
```

## Step 2: Assign the RBAC role

Retrieve the Managed Identity's principal ID and assign *Key Vault Secrets User* (built-in role `4633458b-17de-408a-b874-0445c86b69e6`):

```bash
PRINCIPAL_ID=$(az identity show \
  --name id-app \
  --resource-group rg-rbac-test \
  --query principalId \
  --output tsv)

KV_ID=$(az keyvault show \
  --name kv-rbac-test \
  --resource-group rg-rbac-test \
  --query id \
  --output tsv)

az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "4633458b-17de-408a-b874-0445c86b69e6" \
  --scope "$KV_ID"
```

## Step 3: Read the secret using the SDK

Install the required packages:

```bash
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
```

Read the secret as the assigned Managed Identity using `AzureLocalCredential`, which maps to the principal you specify:

```csharp
using Azure.Security.KeyVault.Secrets;
using Topaz.Identity;

// Retrieve the principal ID of the Managed Identity
var principalId = "<principal-id-from-az-identity-show>";

var vaultUri = new Uri("https://kv-rbac-test.vault.topaz.local.dev:8898");
var credential = new AzureLocalCredential(principalId);
var client = new SecretClient(vaultUri, credential);

// This should succeed — the role assignment grants read access
KeyVaultSecret secret = await client.GetSecretAsync("DatabasePassword");
Console.WriteLine(secret.Value); // super-secret-value
```

## Step 4: Write the negative test

An identity without a role assignment should receive a `403 Forbidden` response:

```csharp
using Azure.RequestFailedException;
using Azure.Security.KeyVault.Secrets;
using Topaz.Identity;
using Xunit;

public class SecretsRbacTests
{
    private const string VaultUri = "https://kv-rbac-test.vault.topaz.local.dev:8898";

    [Fact]
    public async Task AssignedIdentity_CanReadSecret()
    {
        var assignedPrincipalId = "<principal-id-with-role>";
        var client = new SecretClient(
            new Uri(VaultUri),
            new AzureLocalCredential(assignedPrincipalId));

        var secret = await client.GetSecretAsync("DatabasePassword");
        Assert.Equal("super-secret-value", secret.Value.Value);
    }

    [Fact]
    public async Task UnassignedIdentity_IsDenieAccess()
    {
        // A different principal ID that has no role assignment on this vault
        var unassignedPrincipalId = Guid.NewGuid().ToString();
        var client = new SecretClient(
            new Uri(VaultUri),
            new AzureLocalCredential(unassignedPrincipalId));

        var exception = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync("DatabasePassword").AsTask());

        Assert.Equal(403, exception.Status);
    }
}
```

## Step 5: Test from the .NET SDK with role assignment via ARM

If you prefer to create the role assignment programmatically rather than via the CLI, use `Azure.ResourceManager.Authorization`:

```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Topaz.Identity;

var arm = new ArmClient(
    new AzureLocalCredential(),
    "00000000-0000-0000-0000-000000000001");

var subscription = await arm.GetDefaultSubscriptionAsync();
var rg = (await subscription.GetResourceGroupAsync("rg-rbac-test")).Value;

// Key Vault Secrets User built-in role
const string keyVaultSecretsUserRoleId = "4633458b-17de-408a-b874-0445c86b69e6";
var kvResource = arm.GetKeyVaultResource(
    new Azure.Core.ResourceIdentifier(
        $"/subscriptions/00000000-0000-0000-0000-000000000001/resourceGroups/rg-rbac-test/providers/Microsoft.KeyVault/vaults/kv-rbac-test"));

var principalId = Guid.Parse("<managed-identity-principal-id>");
var roleDefinitionId = new Azure.Core.ResourceIdentifier(
    $"/providers/Microsoft.Authorization/roleDefinitions/{keyVaultSecretsUserRoleId}");

await kvResource.GetRoleAssignments().CreateOrUpdateAsync(
    Azure.WaitUntil.Completed,
    Guid.NewGuid().ToString(),
    new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId)
    {
        PrincipalType = RoleManagementPrincipalType.ServicePrincipal
    });
```

:::tip[Switching to production]

The SDK calls, credential pattern, and role assignment IDs are identical to real Azure. The only change is replacing `AzureLocalCredential` with `DefaultAzureCredential` and pointing the vault URI at `https://kv-rbac-test.vault.azure.net`.

:::

:::note[RBAC support]

RBAC in Topaz is partially implemented. Role assignments on Key Vault scopes and resource group scopes are supported. See [RBAC API coverage](../api-coverage/rbac.md) for the full status.

:::
