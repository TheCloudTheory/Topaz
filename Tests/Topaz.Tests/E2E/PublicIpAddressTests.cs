using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class PublicIpAddressTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234000011");

    private const string SubscriptionName = "sub-test-pip";
    private const string ResourceGroupName = "rg-test-pip";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPIsCreated_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string pipName = "test-pip-create";

        var pipData = new PublicIPAddressData
        {
            Location = AzureLocation.WestEurope,
            PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
            PublicIPAddressVersion = NetworkIPVersion.IPv4
        };

        // Act
        var result = await resourceGroup.Value.GetPublicIPAddresses()
            .CreateOrUpdateAsync(WaitUntil.Completed, pipName, pipData);
        var pip = result.Value;

        // Assert
        Assert.That(pip, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(pip.Data.Name, Is.EqualTo(pipName));
            Assert.That(pip.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Network/publicIPAddresses")));
            Assert.That(pip.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(pip.Data.ProvisioningState.ToString(), Is.EqualTo("Succeeded").IgnoreCase);
            Assert.That(pip.Data.IPAddress, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPIsDeleted_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string pipName = "test-pip-delete";

        var pipData = new PublicIPAddressData
        {
            Location = AzureLocation.WestEurope,
            PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
        };

        await resourceGroup.Value.GetPublicIPAddresses()
            .CreateOrUpdateAsync(WaitUntil.Completed, pipName, pipData);

        // Act
        var pip = resourceGroup.Value.GetPublicIPAddress(pipName);
        await pip.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetPublicIPAddressAsync(pipName));
    }

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPsAreListedByResourceGroup_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var pipData = new PublicIPAddressData
        {
            Location = AzureLocation.WestEurope,
            PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
        };

        await resourceGroup.Value.GetPublicIPAddresses()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-pip-list-a", pipData);
        await resourceGroup.Value.GetPublicIPAddresses()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-pip-list-b", pipData);

        // Act
        var pips = resourceGroup.Value.GetPublicIPAddresses().GetAllAsync();

        // Assert
        var names = new List<string>();
        await foreach (var pip in pips)
            names.Add(pip.Data.Name);

        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-pip-list-a"));
            Assert.That(names, Does.Contain("test-pip-list-b"));
        });
    }
}
