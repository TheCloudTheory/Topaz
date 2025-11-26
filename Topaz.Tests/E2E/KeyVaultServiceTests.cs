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
    private const string ResourceGroupName = "rg-test-keyvault";
    
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

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvdeleted";
        
        // Act
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        kv.Value.Delete(WaitUntil.Completed);
        
        // Assert
        Assert.Throws<RequestFailedException>(() => resourceGroup.Value.GetKeyVault(testKeyVaultName));
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsUpdatedUsingSDK_TheProvidedPropertiesShouldBeUpdated()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvupdated";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var updateOperation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
            {
                EnabledForDeployment = true,
                EnabledForDiskEncryption =  true,
                EnabledForTemplateDeployment = true,
                EnableRbacAuthorization = true,
            });
        
        // Act
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, updateOperation, CancellationToken.None);
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
            Assert.That(kv.Value.Data.Properties.EnabledForDeployment, Is.EqualTo(updateOperation.Properties.EnabledForDeployment));
            Assert.That(kv.Value.Data.Properties.EnabledForDiskEncryption, Is.EqualTo(updateOperation.Properties.EnabledForDiskEncryption));
            Assert.That(kv.Value.Data.Properties.EnabledForTemplateDeployment, Is.EqualTo(updateOperation.Properties.EnabledForTemplateDeployment));
            Assert.That(kv.Value.Data.Properties.EnableRbacAuthorization, Is.EqualTo(updateOperation.Properties.EnableRbacAuthorization));
        });
    }
}