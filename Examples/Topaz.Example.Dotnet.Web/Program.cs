using Azure.Core;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Storage.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;

var builder = WebApplication.CreateBuilder(args);
var keyVaultName = builder.Configuration["Azure:KeyVaultName"]!;

if (builder.Environment.IsDevelopment())
{
    var container = new ContainerBuilder()
        .WithImage("thecloudtheory/topaz-cli:v1.0.102-alpha")
        .WithPortBinding(8890)
        .WithPortBinding(8899)
        .WithPortBinding(8898)
        .WithPortBinding(8897)
        .WithPortBinding(8891)
        .Build();

    await container.StartAsync()
        .ConfigureAwait(false);

    await Task.Delay(5000);

    var subscriptionId = Guid.NewGuid();
    const string resourceGroupName = "rg-topaz-webapp-example";
    const string storageAccountName = "storagetopazweb";

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
}

builder.Configuration.AddAzureKeyVault(
    TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName), new AzureLocalCredential());

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/secret", ([FromQuery] string key, IConfiguration configuration) =>
    {
        // You can call that endpoint as e.g. /secret?key=secrets-generic-secret
        // to see the secret value.
        var secret = configuration.GetValue<string>(key);
        return secret;
    })
    .WithName("GetSecretValue")
    .WithOpenApi();

app.Run();
