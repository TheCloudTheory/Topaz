using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class KeyVaultServiceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("727D20F8-F051-41D0-8D00-E93D31E998E8");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkv";
        
        // Act
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        
        // Assert
        Assert.That(kv, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(kv.Value.Data.Name, Is.EqualTo(testKeyVaultName));
            Assert.That(kv.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.KeyVault/vaults")));
            Assert.That(kv.Value.Data.Properties.TenantId, Is.EqualTo(operation.Properties.TenantId));
            Assert.That(kv.Value.Data.Properties.Sku.Family, Is.EqualTo(operation.Properties.Sku.Family));
            Assert.That(kv.Value.Data.Properties.Sku.Name, Is.EqualTo(operation.Properties.Sku.Name));
        });
    }
}