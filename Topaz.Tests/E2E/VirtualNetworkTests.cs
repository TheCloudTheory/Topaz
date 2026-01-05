using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
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
            AddressPrefixes = { "10.0.0.0/22" },
            Subnets =
            {
                new SubnetData
                {
                    AddressPrefixes =
                    {
                        "10.0.0.0/26"
                    },
                    Name = "test-subnet"
                }
            }
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
            Assert.That(virtualNetwork.Value.Data.Subnets, Has.Count.EqualTo(1));
            Assert.That(virtualNetwork.Value.Data.Subnets.First().Name, Is.EqualTo("test-subnet"));
            Assert.That(virtualNetwork.Value.Data.Subnets.First().AddressPrefixes, Contains.Item("10.0.0.0/26"));
        });
    }

    [Test]
    public async Task VirtualNetworkTests_WhenVirtualNetworkIsCreatedUsingTemplate_ItShouldBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub-vnet-deployment";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-vnet";
            
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient();
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-vnet.json"))
            }));
        
        // Assert
        var vnet = await rg.Value.GetVirtualNetworkAsync("topaz-vnet");

        Assert.Multiple(() =>
        {
            Assert.That(vnet, Is.Not.Null);
            Assert.That(vnet.Value.Data.Name, Is.EqualTo("topaz-vnet"));
        });
    }
}