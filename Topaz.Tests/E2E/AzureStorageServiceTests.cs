using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AzureStorageServiceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
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
            ResourceGroupName
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
    public void AzureStorageServiceTests_WhenStorageIsCreated_ItShouldBeAvailableAndThenDeleted()
    {
        // Arrange
        const string storageAccountName = "test";
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var operation = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);
        
        // Act
        _ = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, storageAccountName, operation);
        var storageAccount = resourceGroup.Value.GetStorageAccount(storageAccountName);
        
        // Assert
        Assert.That(storageAccount, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(storageAccount.Value.Data.Name, Is.EqualTo(storageAccountName));
            Assert.That(storageAccount.Value.Data.Kind, Is.EqualTo(StorageKind.StorageV2));
            Assert.That(storageAccount.Value.Data.Sku.Name, Is.EqualTo(StorageSkuName.StandardLrs));
        });
        
        // Act 2
        storageAccount.Value.Delete(WaitUntil.Completed);
        
        // Assert 2
        Assert.Throws<RequestFailedException>(() => resourceGroup.Value.GetStorageAccount(storageAccountName),
            "The Resource 'Microsoft.Storage/storageAccounts/test' under resource group 'test' was not found");
    }

    [Test]
    public void AzureStorageServiceTests_WhenStorageAccountIsCreated_ItShouldHaveTwoAccesKeysAvailable()
    {
        // Arrange
        const string storageAccountName = "test";
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var operation = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);
        
        // Act
        _ = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, storageAccountName, operation);
        var storageAccount = resourceGroup.Value.GetStorageAccount(storageAccountName);
        var keys = storageAccount.Value.GetKeys().ToArray();
        
        // Assert
        Assert.That(keys, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(keys[0].Value, Is.Not.Null);
            Assert.That(keys[0].KeyName, Is.Not.Null);
            Assert.That(keys[0].Permissions, Is.Not.Null);
            Assert.That(keys[0].CreatedOn, Is.Not.Null);
            Assert.That(keys[1].Value, Is.Not.Null);
            Assert.That(keys[1].KeyName, Is.Not.Null);
            Assert.That(keys[1].Permissions, Is.Not.Null);
            Assert.That(keys[1].CreatedOn, Is.Not.Null);
        });
    }
}