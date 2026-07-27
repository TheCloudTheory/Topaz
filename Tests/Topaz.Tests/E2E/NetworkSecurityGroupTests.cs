using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class NetworkSecurityGroupTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("C2A14700-8B1E-41F3-9D5C-3EA7B2C95011");

    private const string SubscriptionName = "sub-test-nsg";
    private const string ResourceGroupName = "rg-test-nsg";

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
    public async Task NetworkSecurityGroup_CreateOrUpdate_ShouldSucceed()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string nsgName = "nsg-create-test";
        var data = new NetworkSecurityGroupData { Location = AzureLocation.WestEurope };

        // Act
        var nsg = await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, nsgName, data, CancellationToken.None);

        // Assert
        Assert.That(nsg, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(nsg.Value.Data.Name, Is.EqualTo(nsgName));
            Assert.That(nsg.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Network/networkSecurityGroups")));
            Assert.That(nsg.Value.Data.DefaultSecurityRules, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public async Task NetworkSecurityGroup_Get_ShouldReturnNetworkSecurityGroup()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string nsgName = "nsg-get-test";
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, nsgName, new NetworkSecurityGroupData { Location = AzureLocation.WestEurope }, CancellationToken.None);

        // Act
        var nsg = await resourceGroup.Value.GetNetworkSecurityGroupAsync(nsgName);

        // Assert
        Assert.That(nsg, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(nsg.Value.Data.Name, Is.EqualTo(nsgName));
            Assert.That(nsg.Value.Data.DefaultSecurityRules, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public async Task NetworkSecurityGroup_Delete_ShouldRemoveNetworkSecurityGroup()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string nsgName = "nsg-delete-test";
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, nsgName, new NetworkSecurityGroupData { Location = AzureLocation.WestEurope }, CancellationToken.None);

        // Act
        var nsgResponse = await resourceGroup.Value.GetNetworkSecurityGroupAsync(nsgName);
        await nsgResponse.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        bool notFound = false;
        try
        {
            await resourceGroup.Value.GetNetworkSecurityGroupAsync(nsgName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            notFound = true;
        }
        Assert.That(notFound, Is.True, "Expected NSG to be deleted (404).");
    }

    [Test]
    public async Task NetworkSecurityGroup_ListByResourceGroup_ShouldReturnNetworkSecurityGroups()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new NetworkSecurityGroupData { Location = AzureLocation.WestEurope };
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, "nsg-list-a", data, CancellationToken.None);
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, "nsg-list-b", data, CancellationToken.None);

        // Act
        var nsgs = await resourceGroup.Value.GetNetworkSecurityGroups().GetAllAsync().ToListAsync();

        // Assert
        Assert.That(nsgs.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(nsgs.Select(n => n.Data.Name), Does.Contain("nsg-list-a"));
        Assert.That(nsgs.Select(n => n.Data.Name), Does.Contain("nsg-list-b"));
    }

    [Test]
    public async Task NetworkSecurityGroup_ListBySubscription_ShouldReturnNetworkSecurityGroups()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new NetworkSecurityGroupData { Location = AzureLocation.WestEurope };
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, "nsg-sub-list-a", data, CancellationToken.None);
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, "nsg-sub-list-b", data, CancellationToken.None);

        // Act
        var nsgs = await subscription.GetNetworkSecurityGroupsAsync().ToListAsync();

        // Assert
        Assert.That(nsgs.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(nsgs.Select(n => n.Data.Name), Does.Contain("nsg-sub-list-a"));
        Assert.That(nsgs.Select(n => n.Data.Name), Does.Contain("nsg-sub-list-b"));
    }

    [Test]
    public async Task NetworkSecurityGroup_UpdateTags_ShouldUpdateTags()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string nsgName = "nsg-tags-test";
        await resourceGroup.Value.GetNetworkSecurityGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, nsgName, new NetworkSecurityGroupData { Location = AzureLocation.WestEurope }, CancellationToken.None);
        var nsgResponse = await resourceGroup.Value.GetNetworkSecurityGroupAsync(nsgName);

        // Act
        var updated = await nsgResponse.Value.UpdateAsync(new NetworkTagsObject
        {
            Tags = { ["env"] = "topaz", ["owner"] = "test" }
        });

        // Assert
        Assert.That(updated, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Value.Data.Tags.ContainsKey("env"), Is.True);
            Assert.That(updated.Value.Data.Tags["env"], Is.EqualTo("topaz"));
            Assert.That(updated.Value.Data.Tags["owner"], Is.EqualTo("test"));
        });
    }
}
