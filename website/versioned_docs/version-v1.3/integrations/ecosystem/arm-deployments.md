---
sidebar_position: 3
slug: /ecosystem/arm-deployments
---

# ARM Template Deployments

Topaz supports ARM template deployments at resource-group scope, using the real `Azure.Deployments` template engine. Template expressions — `[resourceGroup().location]`, `[parameters('name')]`, `[concat(...)]`, `dependsOn`, `outputs`, and more — are evaluated identically to Azure, so templates that work against Topaz work against real Azure without modification.

## Supported resource types

The following resource types are recognised by the Topaz deployment orchestrator. Resources of any other type are skipped with a warning, and the rest of the deployment proceeds normally.

| Resource type | Notes |
|---|---|
| `Microsoft.ContainerRegistry/registries` | ACR Basic / Standard / Premium SKUs |
| `Microsoft.EventHub/namespaces` | Namespace + child `eventhubs` resources |
| `Microsoft.KeyVault/vaults` | Standard and Premium SKUs |
| `Microsoft.ManagedIdentity/userAssignedIdentities` | User-assigned only |
| `Microsoft.Network/virtualNetworks` | VNet with subnet definitions |
| `Microsoft.ServiceBus/namespaces` | Namespace + child queues / topics |
| `Microsoft.Storage/storageAccounts` | StorageV2, BlobStorage, etc. |

## Deployment modes

| Mode | Behaviour |
|---|---|
| `Incremental` (default) | Adds or updates resources in the template; resources already in the resource group but absent from the template are left untouched. |
| `Complete` | Creates / updates resources in the template **and deletes** any resources in the resource group that are not in the template. |

## Topaz CLI

### Create or update a deployment

```bash
topaz deployment group create \
  --subscription-id 00000000-0000-0000-0000-000000000000 \
  --resource-group my-rg \
  --name my-deployment \
  --template-file ./template.json \
  --mode Incremental
```

| Flag | Short | Required | Description |
|---|---|---|---|
| `--subscription-id` | `-s` | ✓ | Subscription ID |
| `--resource-group` | `-g` | ✓ | Resource group name |
| `--name` | `-n` | | Deployment name (auto-generated if omitted) |
| `--template-file` | `-f` | ✓ | Path to ARM JSON template file |
| `--mode` | | | `Incremental` (default) or `Complete` |

### List deployments

```bash
topaz deployment group list \
  --subscription-id 00000000-0000-0000-0000-000000000000 \
  --resource-group my-rg
```

## Azure SDK (.NET)

Use the `Azure.ResourceManager` package directly. Topaz honours the same SDK surface as real Azure.

```csharp
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Identity;

var credential = new AzureLocalCredential();
var client = new ArmClient(credential, "00000000-0000-0000-0000-000000000000",
    new ArmClientOptions { Environment = new ArmEnvironment(new Uri("https://localhost:8899"), "https://management.azure.com/") });

var subscriptionId = "00000000-0000-0000-0000-000000000000";
var rg = await client.GetDefaultSubscription()
    .GetResourceGroups()
    .GetAsync("my-rg");

var templateJson = await File.ReadAllTextAsync("templates/my-template.json");

await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
    WaitUntil.Completed,
    "my-deployment",
    new ArmDeploymentContent(
        new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(templateJson)
        }));

Console.WriteLine("Deployment complete.");
```

### Passing parameters inline

```csharp
await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
    WaitUntil.Completed,
    "my-deployment",
    new ArmDeploymentContent(
        new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(templateJson),
            Parameters = BinaryData.FromObjectAsJson(new
            {
                keyvaultName = new { value = "my-keyvault" },
                managedIdentityName = new { value = "my-identity" }
            })
        }));
```

## Azure CLI

With Topaz [configured as an Azure CLI cloud](../azure-cli-integration.md), the standard `az deployment group create` command works without changes.

```bash
# Simple deployment
az deployment group create \
  --resource-group my-rg \
  --name my-deployment \
  --template-file template.json \
  --mode Incremental

# With a parameters file
az deployment group create \
  --resource-group my-rg \
  --name my-deployment \
  --template-file template.json \
  --parameters @template.parameters.json

# Show deployment status
az deployment group show \
  --resource-group my-rg \
  --name my-deployment

# List all deployments in a resource group
az deployment group list \
  --resource-group my-rg \
  --output table
```

## Template examples

### Key Vault (single resource)

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "resources": [
    {
      "type": "Microsoft.KeyVault/vaults",
      "apiVersion": "2024-11-01",
      "name": "my-keyvault",
      "location": "[resourceGroup().location]",
      "sku": {
        "family": "A",
        "name": "standard"
      },
      "properties": {
        "tenantId": "[tenant().tenantId]",
        "accessPolicies": [],
        "enableRbacAuthorization": true,
        "enableSoftDelete": true,
        "softDeleteRetentionInDays": 90
      }
    }
  ]
}
```

### Multiple resources with parameters

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "keyvaultName": { "type": "string" },
    "managedIdentityName": { "type": "string" }
  },
  "resources": [
    {
      "type": "Microsoft.KeyVault/vaults",
      "apiVersion": "2024-11-01",
      "name": "[parameters('keyvaultName')]",
      "location": "[resourceGroup().location]",
      "sku": { "family": "A", "name": "standard" },
      "properties": {
        "tenantId": "[tenant().tenantId]",
        "accessPolicies": [],
        "enableRbacAuthorization": true,
        "enableSoftDelete": true,
        "softDeleteRetentionInDays": 90
      }
    },
    {
      "type": "Microsoft.ManagedIdentity/userAssignedIdentities",
      "apiVersion": "2023-01-31",
      "name": "[parameters('managedIdentityName')]",
      "location": "[resourceGroup().location]",
      "properties": {}
    }
  ]
}
```

### Parameters file

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "keyvaultName": { "value": "my-keyvault" },
    "managedIdentityName": { "value": "my-identity" }
  }
}
```

### Event Hub namespace with child resource

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "namespaceName": { "type": "string", "defaultValue": "my-eventhub-ns" },
    "eventHubName":  { "type": "string", "defaultValue": "my-hub" }
  },
  "resources": [
    {
      "type": "Microsoft.EventHub/namespaces",
      "apiVersion": "2024-01-01",
      "name": "[parameters('namespaceName')]",
      "location": "[resourceGroup().location]",
      "sku": { "name": "Standard", "tier": "Standard", "capacity": 1 },
      "properties": {}
    },
    {
      "type": "Microsoft.EventHub/namespaces/eventhubs",
      "apiVersion": "2024-01-01",
      "name": "[format('{0}/{1}', parameters('namespaceName'), parameters('eventHubName'))]",
      "dependsOn": [
        "[resourceId('Microsoft.EventHub/namespaces', parameters('namespaceName'))]"
      ],
      "properties": {
        "messageRetentionInDays": 1,
        "partitionCount": 2
      }
    }
  ],
  "outputs": {
    "namespaceName": {
      "type": "string",
      "value": "[parameters('namespaceName')]"
    }
  }
}
```

## Bicep

The Topaz CLI's `--template-file` / `-f` flag accepts Bicep files directly when the `bicep` compiler is available. If you prefer to work with ARM JSON when using the Azure SDK or Azure CLI, compile first:

```bash
az bicep build --file main.bicep --outfile main.json
```

Then deploy the generated `main.json` using any of the methods above.

## Limitations

- **Resource group scope only** — subscription-level and management-group deployments are not supported.
- **Unknown resource types are skipped** — if a template contains a type not in the table above, Topaz logs a warning and moves on. The deployment itself succeeds.
- **Nested / linked deployments** — `Microsoft.Resources/deployments` child templates are not supported.
- **`listKeys` and similar functions** — some template functions that call back into ARM (e.g. `listKeys`) are parsed by the expression engine but may return empty results for resource types not yet fully emulated.
