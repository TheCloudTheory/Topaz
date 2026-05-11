using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
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
    public async Task VirtualNetworkTests_WhenVirtualNetworkIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
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
    public async Task Subnet_CreateOrUpdate_ShouldSucceed()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var vnetData = new VirtualNetworkData { AddressPrefixes = { "10.10.0.0/16" } };
        const string virtualNetworkName = "vnet-subnet-test";
        await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName, vnetData, CancellationToken.None);
        var vnet = resourceGroup.Value.GetVirtualNetwork(virtualNetworkName).Value;

        // Act
        var subnetData = new SubnetData { AddressPrefixes = { "10.10.1.0/24" } };
        var subnet = await vnet.GetSubnets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-subnet", subnetData, CancellationToken.None);

        // Assert
        Assert.That(subnet, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(subnet.Value.Data.Name, Is.EqualTo("test-subnet"));
            Assert.That(subnet.Value.Data.ResourceType,
                Is.EqualTo(new ResourceType("Microsoft.Network/virtualNetworks/subnets")));
            Assert.That(subnet.Value.Data.AddressPrefixes, Contains.Item("10.10.1.0/24"));
        });
    }

    [Test]
    public async Task Subnet_Get_ShouldReturnSubnet()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var vnetData = new VirtualNetworkData { AddressPrefixes = { "10.20.0.0/16" } };
        const string virtualNetworkName = "vnet-get-subnet-test";
        await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName, vnetData, CancellationToken.None);
        var vnet = resourceGroup.Value.GetVirtualNetwork(virtualNetworkName).Value;
        await vnet.GetSubnets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "get-subnet", new SubnetData { AddressPrefixes = { "10.20.1.0/24" } }, CancellationToken.None);

        // Act
        var subnet = await vnet.GetSubnetAsync("get-subnet");

        // Assert
        Assert.That(subnet, Is.Not.Null);
        Assert.That(subnet.Value.Data.Name, Is.EqualTo("get-subnet"));
        Assert.That(subnet.Value.Data.AddressPrefixes, Contains.Item("10.20.1.0/24"));
    }

    [Test]
    public async Task Subnet_Delete_ShouldSucceed()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var vnetData = new VirtualNetworkData { AddressPrefixes = { "10.30.0.0/16" } };
        const string virtualNetworkName = "vnet-delete-subnet-test";
        await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName, vnetData, CancellationToken.None);
        var vnet = resourceGroup.Value.GetVirtualNetwork(virtualNetworkName).Value;
        await vnet.GetSubnets()
            .CreateOrUpdateAsync(WaitUntil.Completed, "del-subnet", new SubnetData { AddressPrefixes = { "10.30.1.0/24" } }, CancellationToken.None);

        // Act
        var subnetResponse = await vnet.GetSubnetAsync("del-subnet");
        await subnetResponse.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        bool notFound = false;
        try
        {
            await vnet.GetSubnetAsync("del-subnet");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            notFound = true;
        }
        Assert.That(notFound, Is.True, "Expected subnet to be deleted (404).");
    }

    [Test]
    public async Task Subnet_List_ShouldReturnSubnets()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var vnetData = new VirtualNetworkData { AddressPrefixes = { "10.40.0.0/16" } };
        const string virtualNetworkName = "vnet-list-subnets-test";
        await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName, vnetData, CancellationToken.None);
        var vnet = resourceGroup.Value.GetVirtualNetwork(virtualNetworkName).Value;
        await vnet.GetSubnets().CreateOrUpdateAsync(WaitUntil.Completed, "subnet-a", new SubnetData { AddressPrefixes = { "10.40.1.0/24" } }, CancellationToken.None);
        await vnet.GetSubnets().CreateOrUpdateAsync(WaitUntil.Completed, "subnet-b", new SubnetData { AddressPrefixes = { "10.40.2.0/24" } }, CancellationToken.None);

        // Act
        var subnets = await vnet.GetSubnets().GetAllAsync().ToListAsync();

        // Assert
        Assert.That(subnets.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(subnets.Select(s => s.Data.Name), Does.Contain("subnet-a"));
        Assert.That(subnets.Select(s => s.Data.Name), Does.Contain("subnet-b"));
    }

    [Test]
    public async Task VirtualNetworkTests_WhenVirtualNetworkIsCreatedUsingTemplate_ItShouldBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub-vnet-deployment";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-vnet";
            
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
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