using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AspNetCoreExtensionTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "testsatopaz";
    private const string KeyVaultName = "kvtesttopaz";

    [Test]
    public async Task WhenStorageAccountConnectionStringIsAddedAsSecret_ItMustBeAvailable()
    {
        // Arrange
        const string secretName = "connectionString-storageAccount";
        var builder = new ConfigurationBuilder();
        
        // Act
        await builder.AddTopaz(SubscriptionId)
            .AddSubscription(SubscriptionId, SubscriptionName)
            .AddResourceGroup(SubscriptionId, ResourceGroupName, AzureLocation.WestEurope)
            .AddStorageAccount(ResourceGroupName, StorageAccountName,
                new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs),
                    StorageKind.StorageV2, AzureLocation.WestEurope))
            .AddKeyVault(ResourceGroupName, KeyVaultName,
                new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                    new KeyVaultProperties(Guid.Empty,
                        new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))))
            .AddStorageAccountConnectionStringAsSecret(ResourceGroupName, StorageAccountName, KeyVaultName,
                secretName);
        
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(KeyVaultName), credential: credentials, new SecretClientOptions
        {
            DisableChallengeResourceVerification = true
        });
        var secret = await client.GetSecretAsync(secretName);
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
        var key = storageAccount.Value.GetKeys().ToArray()[0];

        // Assert
        Assert.That(secret, Is.Not.Null);
        Assert.That(secret.Value, Is.Not.Null);
        Assert.That(secret.Value.Value, Does.Contain(key.Value));
    }
}