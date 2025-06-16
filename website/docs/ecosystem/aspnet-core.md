---
sidebar_position: 1
---

# ASP.NET Core
To simplify integration of Topaz with ASP.NET Core applications, you can take advantage of a dedicated NuGet Package - [TheCloudTheory.Topaz.AspNetCore.Extensions](https://www.nuget.org/packages/TheCloudTheory.Topaz.AspNetCore.Extensions/). This package offers a seamless integration with the framework by providing a fluent API for creating and managing the emulated infrastructure.

```csharp

var builder = WebApplication.CreateBuilder(args);

var subscriptionId = Guid.NewGuid();
const string resourceGroupName = "rg-topaz-webapp-example";

await builder.Configuration.AddTopaz(subscriptionId)
    .AddSubscription(subscriptionId, "topaz-webapp-example")
    .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
    .AddStorageAccount(resourceGroupName, storageAccountName,
        new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2,
            AzureLocation.WestEurope))
    .AddKeyVault(resourceGroupName, keyVaultName,
        new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))),
        secrets: new Dictionary<string, string>
        {
            { "secrets-generic-secret", "This is just example secret!" }
        })
    .AddStorageAccountConnectionStringAsSecret(resourceGroupName, storageAccountName, keyVaultName,
        "connectionstring-storageaccount");
```

To ensure that your application can run locally without a manual intervention (i.e. explicitly running Topaz in the background), you can leverage Testcontainers and run it just before Topaz API:

```csharp
var container = new ContainerBuilder()
        .WithImage("thecloudtheory/topaz-cli:<selected-tag>")
        .WithPortBinding(8890)
        .WithPortBinding(8899)
        .WithPortBinding(8898)
        .WithPortBinding(8897)
        .WithPortBinding(8891)
        .Build();

await container.StartAsync()
    .ConfigureAwait(false);
```

Note the order of the API methods matters, i.e. if you want to place a secret in a Key Vault, Key Vault must be created before that. Currently the API isn't validating the correctness of the setup, but any misconfiguration will likely cause an exception before the application starts.