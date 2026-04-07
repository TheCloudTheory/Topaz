---
sidebar_position: 1
---

# ASP.NET Core

The [TheCloudTheory.Topaz.AspNetCore.Extensions](https://www.nuget.org/packages/TheCloudTheory.Topaz.AspNetCore.Extensions/) NuGet package provides a fluent builder API that lets your ASP.NET Core application provision its own local Azure infrastructure at startup — no manual CLI steps required.

## Installation

```bash
dotnet add package TheCloudTheory.Topaz.AspNetCore.Extensions
```

You will also need the Azure SDK packages that match the services you want to provision:

```bash
dotnet add package Azure.ResourceManager.KeyVault
dotnet add package Azure.ResourceManager.Storage
dotnet add package Azure.ResourceManager.ServiceBus
```

## How it works

Call `AddTopaz(subscriptionId, objectId)` on `IConfigurationBuilder` to get a `TopazEnvironmentBuilder`. Chain builder methods on the returned task to create resources in dependency order. The configuration values produced (e.g. connection strings, secret URIs) are injected into the application's `IConfiguration`.

:::info[Order matters]

Resources must be created before they are referenced. For example, a Key Vault must exist before you can write a secret into it, and a Storage Account must exist before its connection string can be stored as a Key Vault secret.

:::

## Builder API reference

| Method | Description |
|---|---|
| `AddTopaz(subscriptionId, objectId)` | Entry point — returns a `TopazEnvironmentBuilder` |
| `.AddSubscription(subscriptionId, name, credentials)` | Creates a subscription in Topaz |
| `.AddResourceGroup(subscriptionId, name, location)` | Creates or updates a resource group |
| `.AddStorageAccount(resourceGroup, name, content)` | Creates or updates a Storage Account |
| `.AddKeyVault(resourceGroup, name, content)` | Creates or updates a Key Vault |
| `.AddKeyVault(resourceGroup, name, content, secrets, objectId)` | Creates a Key Vault and populates it with secrets |
| `.AddStorageAccountConnectionStringAsSecret(resourceGroup, storageAccount, keyVault, secretName, objectId)` | Reads the Storage Account key and stores the connection string as a Key Vault secret |
| `.AddServiceBusNamespace(resourceGroup, namespace, data)` | Creates or updates a Service Bus namespace |
| `.AddServiceBusQueue(resourceGroup, namespace, queueName, data)` | Creates or updates a queue inside a Service Bus namespace |
| `.AddServiceBusTopic(resourceGroup, namespace, topicName, data)` | Creates or updates a topic inside a Service Bus namespace |

The `objectId` parameter is the Entra ID object ID of the calling principal. Pass `Guid.Empty.ToString()` to act as a superadmin with no permission restrictions during local development.

## Complete example

The following snippet shows a realistic startup that provisions a subscription, resource group, Storage Account, Key Vault with secrets, and stores the Storage Account connection string as a Key Vault secret:

```csharp
using Azure.Core;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Storage.Models;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;

var builder = WebApplication.CreateBuilder(args);

var subscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
const string objectId = "00000000-0000-0000-0000-000000000000"; // superadmin
const string resourceGroupName = "rg-my-app";
const string storageAccountName = "stmyapp001";
const string keyVaultName = "kv-my-app";

await builder.Configuration
    .AddTopaz(subscriptionId, objectId)
    .AddSubscription(subscriptionId, "dev-local", new AzureLocalCredential(objectId))
    .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
    .AddStorageAccount(
        resourceGroupName,
        storageAccountName,
        new StorageAccountCreateOrUpdateContent(
            new StorageSku(StorageSkuName.StandardLrs),
            StorageKind.StorageV2,
            AzureLocation.WestEurope))
    .AddKeyVault(
        resourceGroupName,
        keyVaultName,
        new KeyVaultCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new KeyVaultProperties(
                Guid.Empty,
                new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))),
        secrets: new Dictionary<string, string>
        {
            { "my-app-setting", "hello-from-topaz" }
        },
        objectId: objectId)
    .AddStorageAccountConnectionStringAsSecret(
        resourceGroupName,
        storageAccountName,
        keyVaultName,
        secretName: "connectionstring-storage",
        objectId: objectId);

var app = builder.Build();
// ...
app.Run();
```

## Running Topaz automatically with Testcontainers

To ensure the emulator is running without any manual steps — ideal for tests or `dotnet run` workflows — spin up the Topaz container before calling `AddTopaz`:

```csharp
using DotNet.Testcontainers.Builders;

// Start Topaz before provisioning infrastructure
var topazContainer = await new ContainerBuilder()
    .WithImage("thecloudtheory/topaz-cli:<tag>")
    .WithPortBinding(8899)   // ARM / Resource Manager
    .WithPortBinding(8898)   // Key Vault
    .WithPortBinding(8891)   // Blob Storage
    .WithPortBinding(8890)   // Table Storage
    .WithPortBinding(8897)   // Event Hub (HTTP)
    .WithCommand("start", "--log-level", "Information")
    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
    .Build();

await topazContainer.StartAsync();

// Now provision infrastructure
await builder.Configuration
    .AddTopaz(subscriptionId, objectId)
    .AddSubscription(/* ... */);
```

:::tip[Testcontainers NuGet package]

```bash
dotnet add package Testcontainers
```

:::

## Service Bus example

```csharp
using Azure.ResourceManager.ServiceBus.Models;
using Azure.ResourceManager.ServiceBus;

await builder.Configuration
    .AddTopaz(subscriptionId, objectId)
    .AddSubscription(subscriptionId, "dev-local", new AzureLocalCredential(objectId))
    .AddResourceGroup(subscriptionId, "rg-messaging", AzureLocation.WestEurope)
    .AddServiceBusNamespace(
        "rg-messaging",
        "sb-local",
        new ServiceBusNamespaceData(AzureLocation.WestEurope)
        {
            Sku = new ServiceBusSku(ServiceBusSkuName.Standard)
        })
    .AddServiceBusQueue(
        "rg-messaging",
        "sb-local",
        "orders",
        new ServiceBusQueueData())
    .AddServiceBusTopic(
        "rg-messaging",
        "sb-local",
        "events",
        new ServiceBusTopicData());
```
