using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Security.KeyVault.Secrets;
using DotNet.Testcontainers.Builders;
using JetBrains.Annotations;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Example.Dotnet;

[UsedImplicitly]
internal class Program
{
    public static async Task Main()
    {
        // Create a builder for Topaz container image and all the ports
        // which are exposed by the emulator. Note you don't need to expose
        // every port available.
        var container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:v1.0.90-alpha")
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .Build();
        
        try
        {
            Console.WriteLine("Topaz Example - .NET");
        
            var subscriptionId = Guid.NewGuid();
            const string subscriptionName = "topaz.example";
            const string resourceGroupName = "rg-topaz-example";
            const string keyVaultName = "kvtopazexample";
            const string storageAccountName = "storagetopazexample";
    
            await container.StartAsync()
                .ConfigureAwait(false);
            
            // We added 5000ms of wait statement just to make sure Topaz
            // is already running. Note there's most likely a much better way
            // to do that via wait strategy from TestContainers.
            await Task.Delay(5000);

            await CreateSubscription(subscriptionId, subscriptionName);
            
            var credentials = new AzureLocalCredential();
            var armClient = new ArmClient(credentials, subscriptionId.ToString(), TopazArmClientOptions.New);
            
            var resourceGroup = await CreateResourceGroup(armClient, resourceGroupName);
            
            await CreateKeyVault(resourceGroup.Value, keyVaultName);
            await CreateAzureStorageAccount(resourceGroup.Value, storageAccountName);
            await CreateKeyVaultSecrets(keyVaultName);
        }
        finally
        {
            await container.StopAsync();
        }
    }

    private static async Task CreateAzureStorageAccount(ResourceGroupResource resourceGroup, string storageAccountName)
    {
        var operation = new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs),
            StorageKind.StorageV2, AzureLocation.WestEurope);

        _ = await resourceGroup.GetStorageAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, storageAccountName, operation);
        
        Console.WriteLine($"Azure Storage created successfully!");
    }

    private static async Task CreateKeyVaultSecrets(string keyVaultName)
    {
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName), credential: credentials, new SecretClientOptions
        {
            DisableChallengeResourceVerification = true
        });
        
        await client.SetSecretAsync("secret-name", "test");
        await client.SetSecretAsync("secret-name2", "test2");
        await client.SetSecretAsync("secret-name3", "test3");
        
        Console.WriteLine($"Azure Key Vault secrets created successfully!");
    }

    private static async Task CreateKeyVault(ResourceGroupResource resourceGroup, string keyVaultName)
    {
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        
        _ = await resourceGroup.GetKeyVaults()
            .CreateOrUpdateAsync(WaitUntil.Completed, keyVaultName, operation, CancellationToken.None);
        
        Console.WriteLine($"Azure Key Vault [{keyVaultName}] created successfully!");
    }

    private static async Task<Response<ResourceGroupResource>> CreateResourceGroup(ArmClient armClient, string resourceGroupName)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();
        
        _ = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(AzureLocation.WestEurope));
        
        Console.WriteLine($"Resource group [{resourceGroupName}] created successfully!");
        
        var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);
        return resourceGroup;
    }

    private static async Task CreateSubscription(Guid subscriptionId, string subscriptionName)
    {
        using var topaz = new TopazArmClient();
        
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        
        Console.WriteLine($"Subscription [{subscriptionId}, {subscriptionName}] created successfully!");
    }
}