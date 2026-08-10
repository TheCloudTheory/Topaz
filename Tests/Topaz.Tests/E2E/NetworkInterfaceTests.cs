using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class NetworkInterfaceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234000010");

    private const string SubscriptionName = "sub-test-nic";
    private const string ResourceGroupName = "rg-test-nic";
    private const string VNetName = "vnet-test-nic";
    private const string SubnetName = "default";

    private ResourceIdentifier _subnetId = null!;

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var vnetData = new VirtualNetworkData
        {
            Location = AzureLocation.WestEurope,
            AddressPrefixes = { "10.10.0.0/16" },
            Subnets =
            {
                new SubnetData { Name = SubnetName, AddressPrefixes = { "10.10.0.0/24" } }
            }
        };
        var vnet = await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, VNetName, vnetData);

        _subnetId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Network/virtualNetworks/{VNetName}/subnets/{SubnetName}");
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenNICIsCreated_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string nicName = "test-nic-create";

        var nicData = new NetworkInterfaceData
        {
            Location = AzureLocation.WestEurope,
            IPConfigurations =
            {
                new NetworkInterfaceIPConfigurationData
                {
                    Name = "ipconfig1",
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Subnet = new SubnetData { Id = _subnetId }
                }
            }
        };

        // Act
        var result = await resourceGroup.Value.GetNetworkInterfaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, nicName, nicData);
        var nic = result.Value;

        // Assert
        Assert.That(nic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(nic.Data.Name, Is.EqualTo(nicName));
            Assert.That(nic.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Network/networkInterfaces")));
            Assert.That(nic.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(nic.Data.ProvisioningState.ToString(), Is.EqualTo("Succeeded").IgnoreCase);
        });
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenNICIsDeleted_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string nicName = "test-nic-delete";

        var nicData = new NetworkInterfaceData
        {
            Location = AzureLocation.WestEurope,
            IPConfigurations =
            {
                new NetworkInterfaceIPConfigurationData
                {
                    Name = "ipconfig1",
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Subnet = new SubnetData { Id = _subnetId }
                }
            }
        };

        await resourceGroup.Value.GetNetworkInterfaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, nicName, nicData);

        // Act
        var nic = resourceGroup.Value.GetNetworkInterface(nicName);
        await nic.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetNetworkInterfaceAsync(nicName));
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenNICsAreListedByResourceGroup_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var nicData = new NetworkInterfaceData
        {
            Location = AzureLocation.WestEurope,
            IPConfigurations =
            {
                new NetworkInterfaceIPConfigurationData
                {
                    Name = "ipconfig1",
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Subnet = new SubnetData { Id = _subnetId }
                }
            }
        };

        await resourceGroup.Value.GetNetworkInterfaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-nic-list-a", nicData);
        await resourceGroup.Value.GetNetworkInterfaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-nic-list-b", nicData);

        // Act
        var nics = resourceGroup.Value.GetNetworkInterfaces().GetAllAsync();

        // Assert
        var names = new List<string>();
        await foreach (var nic in nics)
            names.Add(nic.Data.Name);

        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-nic-list-a"));
            Assert.That(names, Does.Contain("test-nic-list-b"));
        });
    }
}
