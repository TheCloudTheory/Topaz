using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AzureStorageServiceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("09265DD3-8B4C-4709-AFC3-90F24878B2DA");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "azurestoragetests";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var operation = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);
        
        // Act
        _ = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, operation);
        var storageAccount = resourceGroup.Value.GetStorageAccount(StorageAccountName);
        
        // Assert
        Assert.That(storageAccount, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(storageAccount.Value.Data.Name, Is.EqualTo(StorageAccountName));
            Assert.That(storageAccount.Value.Data.Kind, Is.EqualTo(StorageKind.StorageV2));
            Assert.That(storageAccount.Value.Data.Sku.Name, Is.EqualTo(StorageSkuName.StandardLrs));
        });
        
        // Act 2
        storageAccount.Value.Delete(WaitUntil.Completed);
        
        // Assert 2
        Assert.Throws<RequestFailedException>(() => resourceGroup.Value.GetStorageAccount(StorageAccountName),
            "The Resource 'Microsoft.Storage/storageAccounts/test' under resource group 'test' was not found");
    }

    [Test]
    public void AzureStorageServiceTests_WhenStorageAccountIsCreated_ItShouldHaveTwoAccesKeysAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var operation = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);
        
        // Act
        _ = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, operation);
        var storageAccount = resourceGroup.Value.GetStorageAccount(StorageAccountName);
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

    [Test]
    public void StorageAccount_ListBySubscription_ReturnsAllAccountsInSubscription()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var createContent = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);

        _ = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, createContent);

        // Act
        var accounts = subscription.GetStorageAccounts().ToArray();

        // Assert
        Assert.That(accounts, Is.Not.Empty);
        Assert.That(accounts.Any(a => a.Data.Name == StorageAccountName), Is.True);
    }

    [Test]
    public void StorageAccount_CheckNameAvailability_ReturnsExpectedAvailability()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var createContent = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);

        var availableResult = subscription.CheckStorageAccountNameAvailability(
            new StorageAccountNameAvailabilityContent("storcheckavail123"));

        Assert.Multiple(() =>
        {
            Assert.That(availableResult.Value.IsNameAvailable, Is.True);
            Assert.That(availableResult.Value.Reason, Is.Null);
        });

        _ = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, createContent);

        var unavailableResult = subscription.CheckStorageAccountNameAvailability(
            new StorageAccountNameAvailabilityContent(StorageAccountName));

        Assert.Multiple(() =>
        {
            Assert.That(unavailableResult.Value.IsNameAvailable, Is.False);
            Assert.That(unavailableResult.Value.Reason?.ToString(), Is.EqualTo("AlreadyExists"));
        });
    }

    [Test]
    public void StorageAccount_Update_AppliesTagsAndPreservesKeys()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var createContent = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);

        var created = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, createContent);
        var originalKeys = created.Value.GetKeys().ToArray();

        // Act — update tags via PATCH
        var patch = new StorageAccountPatch();
        patch.Tags["env"] = "test";
        var updated = created.Value.Update(patch);

        // Assert — tags applied
        Assert.That(updated.Value.Data.Tags.ContainsKey("env"), Is.True);
        Assert.That(updated.Value.Data.Tags["env"], Is.EqualTo("test"));

        // Assert — keys preserved
        var updatedKeys = updated.Value.GetKeys().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(updatedKeys[0].Value, Is.EqualTo(originalKeys[0].Value));
            Assert.That(updatedKeys[1].Value, Is.EqualTo(originalKeys[1].Value));
        });
    }

    [Test]
    public void StorageAccount_RegenerateKey_ReturnsNewKeyValueAndPreservesOtherKey()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var createContent = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);

        var created = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, createContent);
        var originalKeys = created.Value.GetKeys().ToArray();

        // Act
        var regeneratedKeys = created.Value.RegenerateKey(new StorageAccountRegenerateKeyContent("key1")).ToArray();

        // Assert — key1 changed
        Assert.That(regeneratedKeys[0].Value, Is.Not.EqualTo(originalKeys[0].Value));
        // Assert — key2 unchanged
        Assert.That(regeneratedKeys[1].Value, Is.EqualTo(originalKeys[1].Value));
        Assert.Multiple(() =>
        {
            Assert.That(regeneratedKeys[0].KeyName, Is.EqualTo("key1"));
            Assert.That(regeneratedKeys[1].KeyName, Is.EqualTo("key2"));
        });
    }

    [Test]
    public void StorageAccount_ListAccountSas_ReturnsTokenWithExpectedParameters()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var createContent = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);

        var created = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, createContent);

        var sasParams = new AccountSasContent(
            StorageAccountSasSignedService.B,
            StorageAccountSasSignedResourceType.S,
            StorageAccountSasPermission.R,
            DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var sasToken = created.Value.GetAccountSas(sasParams).Value.AccountSasToken;

        // Assert
        Assert.That(sasToken, Is.Not.Null.And.Not.Empty);
        Assert.That(sasToken, Does.Contain("sv="));
        Assert.That(sasToken, Does.Contain("ss="));
        Assert.That(sasToken, Does.Contain("srt="));
        Assert.That(sasToken, Does.Contain("sp="));
        Assert.That(sasToken, Does.Contain("se="));
        Assert.That(sasToken, Does.Contain("sig="));
    }

    [Test]
    public void StorageAccount_ListServiceSas_ReturnsTokenWithExpectedParameters()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var createContent = new StorageAccountCreateOrUpdateContent(sku,
            StorageKind.StorageV2, AzureLocation.WestEurope);

        var created = resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, StorageAccountName, createContent);

        var sasParams = new ServiceSasContent($"/blob/{StorageAccountName}/mycontainer")
        {
            Resource = ServiceSasSignedResourceType.Container,
            Permissions = StorageAccountSasPermission.R,
            SharedAccessExpiryOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Act
        var sasToken = created.Value.GetServiceSas(sasParams).Value.ServiceSasToken;

        // Assert
        Assert.That(sasToken, Is.Not.Null.And.Not.Empty);
        Assert.That(sasToken, Does.Contain("sv="));
        Assert.That(sasToken, Does.Contain("sr="));
        Assert.That(sasToken, Does.Contain("sp="));
        Assert.That(sasToken, Does.Contain("se="));
        Assert.That(sasToken, Does.Contain("sig="));
    }
}