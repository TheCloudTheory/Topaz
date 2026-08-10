---
sidebar_position: 12
description: Develop Azure Functions locally with Service Bus triggers, Key Vault references, and Cosmos DB output — all against Topaz, with no real Azure subscription needed.
keywords: [azure functions local development, azure functions topaz, azure functions service bus local, azure functions key vault local, azure functions cosmos db local, local azure functions emulator]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Local Azure Functions development with Topaz

In this tutorial, we will configure an Azure Functions project to use Topaz as the local backend for all Azure service bindings. The function receives messages from a Service Bus queue, reads a configuration secret from Key Vault, and writes output to Cosmos DB — all without a real Azure subscription.

A complete runnable example is available in [`Examples/Topaz.Example.Functions`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.Functions).

## What you will build

- A Service Bus queue that triggers the function
- A Key Vault secret consumed by the function at runtime
- A Cosmos DB container that receives processed documents
- A `local.settings.json` wired to Topaz endpoints

## Prerequisites

- Topaz installed and running (see [Getting started](../intro.md))
- DNS setup completed and Topaz certificate trusted
- Azure CLI installed (`az --version`) and Topaz cloud registered
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) installed (`func --version`)
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

## Step 1: Provision resources

Create the required Azure resources:

az group create \
  --name rg-functions-local \
  --location westeurope

# Service Bus
az servicebus namespace create \
  --name sbns-orders \
  --resource-group rg-functions-local \
  --location westeurope \
  --sku Standard

az servicebus queue create \
  --name order-requests \
  --namespace-name sbns-orders \
  --resource-group rg-functions-local

# Key Vault
az keyvault create \
  --name kv-functions-local \
  --resource-group rg-functions-local \
  --location westeurope

az keyvault secret set \
  --vault-name kv-functions-local \
  --name ProcessingConfig \
  --value "max-retries=3,timeout=30"

# Cosmos DB
az cosmosdb create \
  --name cosmos-orders \
  --resource-group rg-functions-local \
  --locations regionName=westeurope failoverPriority=0

az cosmosdb sql database create \
  --account-name cosmos-orders \
  --resource-group rg-functions-local \
  --name orders-db

az cosmosdb sql container create \
  --account-name cosmos-orders \
  --resource-group rg-functions-local \
  --database-name orders-db \
  --name processed-orders \
  --partition-key-path /id
```

## Step 2: Create the Functions project

```bash
func init Topaz.Example.Functions --worker-runtime dotnet-isolated --target-framework net10.0
cd Topaz.Example.Functions

dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.CosmosDB
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
```

## Step 3: Configure local.settings.json

Replace `local.settings.json` with Topaz connection strings. Retrieve the Service Bus and Cosmos DB connection strings from Topaz:

```bash
# Service Bus connection string (Topaz returns the standard format)
az servicebus namespace authorization-rule keys list \
  --namespace-name sbns-orders \
  --resource-group rg-functions-local \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv

# Cosmos DB connection string
az cosmosdb keys list \
  --name cosmos-orders \
  --resource-group rg-functions-local \
  --type connection-strings \
  --query connectionStrings[0].connectionString \
  --output tsv
```

Create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",

    "ServiceBusConnection": "Endpoint=sb://sbns-orders.servicebus.topaz.local.dev:8889;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",

    "CosmosDbConnection": "AccountEndpoint=https://cosmos-orders.documents.topaz.local.dev:8895/;AccountKey=<key-from-above>;",

    "KeyVaultUri": "https://kv-functions-local.vault.topaz.local.dev:8898"
  }
}
```

:::note[Service Bus connection string format]

The `UseDevelopmentEmulator=true` flag tells the Azure SDK to connect to Topaz's plain AMQP port (8889) instead of the standard AMQPS port. This is the same flag used by the official Microsoft Service Bus emulator.

:::

## Step 4: Write the function

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;

public class OrderProcessor
{
    private readonly ILogger<OrderProcessor> _logger;
    private readonly SecretClient _secretClient;
    private readonly CosmosClient _cosmosClient;

    public OrderProcessor(
        ILogger<OrderProcessor> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        var keyVaultUri = configuration["KeyVaultUri"]!;
        _secretClient = new SecretClient(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());

        var cosmosConnection = configuration["CosmosDbConnection"]!;
        _cosmosClient = new CosmosClient(
            cosmosConnection,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true
            });
    }

    [Function("OrderProcessor")]
    public async Task Run(
        [ServiceBusTrigger("order-requests", Connection = "ServiceBusConnection")]
        string messageBody)
    {
        _logger.LogInformation("Processing order: {Body}", messageBody);

        // Read configuration from Key Vault
        var configSecret = await _secretClient.GetSecretAsync("ProcessingConfig");
        _logger.LogInformation("Config: {Config}", configSecret.Value.Value);

        // Write to Cosmos DB
        var container = _cosmosClient
            .GetDatabase("orders-db")
            .GetContainer("processed-orders");

        var document = new
        {
            id = Guid.NewGuid().ToString(),
            body = messageBody,
            processedAt = DateTimeOffset.UtcNow,
            config = configSecret.Value.Value
        };

        await container.UpsertItemAsync(document, new PartitionKey(document.id));

        _logger.LogInformation("Order written to Cosmos DB: {Id}", document.id);
    }
}
```

## Step 5: Run the function host

```bash
func host start
```

The function host will start and display the `OrderProcessor` trigger listening on the `order-requests` queue.

## Step 6: Send a test message

In a second terminal, send a message to the queue to trigger the function:

```bash
az servicebus queue send \
  --namespace-name sbns-orders \
  --resource-group rg-functions-local \
  --queue-name order-requests \
  --body '{"orderId":"ord-001","amount":99.99}'
```

Expected function log output:

```
[Information] Processing order: {"orderId":"ord-001","amount":99.99}
[Information] Config: max-retries=3,timeout=30
[Information] Order written to Cosmos DB: <generated-guid>
```

## Step 7: Verify the Cosmos DB document

```bash
az cosmosdb sql container show-throughput \
  --account-name cosmos-orders \
  --resource-group rg-functions-local \
  --database-name orders-db \
  --name processed-orders
```

Or query the container directly using the Azure SDK:

```csharp
var container = cosmosClient
    .GetDatabase("orders-db")
    .GetContainer("processed-orders");

var query = new QueryDefinition("SELECT * FROM c");
using var iterator = container.GetItemQueryIterator<dynamic>(query);

while (iterator.HasMoreResults)
{
    foreach (var item in await iterator.ReadNextAsync())
    {
        Console.WriteLine(item.id);
    }
}
```

:::tip[Switching to production]

Replace the three values in `local.settings.json` with real Azure connection strings and the Key Vault URI. Your function code, bindings, and Cosmos DB queries are unchanged.

:::

:::tip[Integration testing the function]

Use Testcontainers to start Topaz and the Azure Functions runtime in your CI pipeline. See [How to write integration tests with Testcontainers](../ecosystem/testcontainers.md) for the shared fixture pattern.

:::
