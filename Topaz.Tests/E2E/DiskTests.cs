using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class DiskTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234560099");

    private const string SubscriptionName = "sub-test-disk";
    private const string ResourceGroupName = "rg-test-disk";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group", "delete",
            "--name", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    private static ManagedDiskData MinimalDiskData() =>
        new(AzureLocation.WestEurope)
        {
            Sku = new DiskSku { Name = DiskStorageAccountType.PremiumLrs },
            DiskSizeGB = 32,
            CreationData = new DiskCreationData(DiskCreateOption.Empty)
        };

    [Test]
    public async Task DiskTests_WhenDiskIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-create";

        // Act
        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        var disk = createResult.Value;

        // Assert
        Assert.That(disk, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(disk.Data.Name, Is.EqualTo(diskName));
            Assert.That(disk.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Compute/disks")));
            Assert.That(disk.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(disk.Data.ProvisioningState, Is.EqualTo("Succeeded"));
        });
    }

    [Test]
    public async Task DiskTests_WhenDiskIsRetrievedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-get";

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        // Act
        var disk = await resourceGroup.Value.GetManagedDiskAsync(diskName);

        // Assert
        Assert.That(disk.Value.Data.Name, Is.EqualTo(diskName));
    }

    [Test]
    public async Task DiskTests_WhenDiskIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-delete";

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        // Act
        var disk = resourceGroup.Value.GetManagedDisk(diskName);
        await disk.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetManagedDiskAsync(diskName));
    }

    [Test]
    public async Task DiskTests_WhenDiskTagsAreUpdatedUsingSDK_TagsShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-update";

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        // Act
        var patch = new ManagedDiskPatch();
        patch.Tags.Add("env", "test");
        patch.Tags.Add("team", "platform");

        var disk = resourceGroup.Value.GetManagedDisk(diskName);
        var updateResult = await disk.Value.UpdateAsync(WaitUntil.Completed, patch);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.Value.Data.Tags.ContainsKey("env"), Is.True);
            Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("test"));
            Assert.That(updateResult.Value.Data.Tags["team"], Is.EqualTo("platform"));
        });
    }

    [Test]
    public async Task DiskTests_WhenDisksAreListedByResourceGroupUsingSDK_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-disk-list-a", MinimalDiskData());
        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-disk-list-b", MinimalDiskData());

        // Act
        var disks = resourceGroup.Value.GetManagedDisks().GetAll().ToList();

        // Assert
        var names = disks.Select(d => d.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-disk-list-a"));
            Assert.That(names, Does.Contain("test-disk-list-b"));
        });
    }

    [Test]
    public async Task DiskTests_WhenDisksAreListedBySubscriptionUsingSDK_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-disk-sub-a", MinimalDiskData());

        // Act
        var disks = subscription.GetManagedDisks().ToList();

        // Assert
        var names = disks.Select(d => d.Data.Name).ToList();
        Assert.That(names, Does.Contain("test-disk-sub-a"));
    }
}
