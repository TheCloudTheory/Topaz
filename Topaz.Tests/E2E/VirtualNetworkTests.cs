using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class VirtualNetworkTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("50162600-7A5E-459F-95E8-B67B7283B1BD");
    
    private const string SubscriptionName = "sub-test-vnet";
    private const string ResourceGroupName = "rg-test-vnet";
    
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
    public async Task VirtualNetworkTests_WhenVirtualNetworkIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new VirtualNetworkData
        {
            AddressPrefixes = { "10.0.0.0/22" }
        };
        const string virtualNetworkName = "virtual-network";
        
        // Act
        var virtualNetwork = await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName, data, CancellationToken.None);
        
        // Assert
        Assert.That(virtualNetwork, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(virtualNetwork.Value.Data.Name, Is.EqualTo(virtualNetworkName));
            Assert.That(virtualNetwork.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Network/virtualNetworks")));
            Assert.That(virtualNetwork.Value.Data.AddressPrefixes, Contains.Item("10.0.0.0/22"));
        });
    }
}