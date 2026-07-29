using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AvailabilitySetTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234560101");

    private const string SubscriptionName = "sub-test-availability-set";
    private const string ResourceGroupName = "rg-test-availability-set";

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

    private static AvailabilitySetData MinimalAvailabilitySetData() =>
        new(AzureLocation.WestEurope)
        {
            PlatformFaultDomainCount = 2,
            PlatformUpdateDomainCount = 5
        };

    [Test]
    public async Task AvailabilitySet_Create_ReturnsCreated()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string name = "test-avset-create";

        // Act
        var result = await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, name, MinimalAvailabilitySetData());

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(name));
            Assert.That(result.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Compute/availabilitySets")));
            Assert.That(result.Value.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
        }
    }

    [Test]
    public async Task AvailabilitySet_Get_ReturnsAvailabilitySet()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string name = "test-avset-get";

        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, name, MinimalAvailabilitySetData());

        // Act
        var result = await resourceGroup.Value.GetAvailabilitySetAsync(name);

        // Assert
        Assert.That(result.Value.Data.Name, Is.EqualTo(name));
    }

    [Test]
    public async Task AvailabilitySet_Delete_RemovesAvailabilitySet()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string name = "test-avset-delete";

        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, name, MinimalAvailabilitySetData());

        // Act
        var avSet = resourceGroup.Value.GetAvailabilitySet(name);
        await avSet.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetAvailabilitySetAsync(name));
    }

    [Test]
    public async Task AvailabilitySet_Update_PersistsTags()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string name = "test-avset-update";

        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, name, MinimalAvailabilitySetData());

        // Act
        var patch = new AvailabilitySetPatch();
        patch.Tags.Add("env", "test");
        patch.Tags.Add("team", "platform");

        var avSet = await resourceGroup.Value.GetAvailabilitySetAsync(name);
        var updateResult = await avSet.Value.UpdateAsync(patch);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(updateResult.Value.Data.Tags.ContainsKey("env"), Is.True);
            Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("test"));
            Assert.That(updateResult.Value.Data.Tags["team"], Is.EqualTo("platform"));
        }
    }

    [Test]
    public async Task AvailabilitySet_ListByResourceGroup_ReturnsAll()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-avset-list-a", MinimalAvailabilitySetData());
        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-avset-list-b", MinimalAvailabilitySetData());

        // Act
        var sets = resourceGroup.Value.GetAvailabilitySets().GetAll().ToList();

        // Assert
        var names = sets.Select(s => s.Data.Name).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain("test-avset-list-a"));
            Assert.That(names, Does.Contain("test-avset-list-b"));
        }
    }

    [Test]
    public async Task AvailabilitySet_ListBySubscription_ReturnsAll()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-avset-sub-a", MinimalAvailabilitySetData());
        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-avset-sub-b", MinimalAvailabilitySetData());

        // Act
        var sets = subscription.GetAvailabilitySets().ToList();

        // Assert
        var names = sets.Select(s => s.Data.Name).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain("test-avset-sub-a"));
            Assert.That(names, Does.Contain("test-avset-sub-b"));
        }
    }

    [Test]
    public async Task AvailabilitySet_ListAvailableSizes_ReturnsSizes()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string name = "test-avset-sizes";

        await resourceGroup.Value.GetAvailabilitySets()
            .CreateOrUpdateAsync(WaitUntil.Completed, name, MinimalAvailabilitySetData());

        // Act
        var avSet = await resourceGroup.Value.GetAvailabilitySetAsync(name);
        var sizes = avSet.Value.GetAvailableSizes().ToList();

        // Assert
        Assert.That(sizes, Is.Not.Empty);
    }
}
