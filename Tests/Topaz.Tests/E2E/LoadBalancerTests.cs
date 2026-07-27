using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class LoadBalancerTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D1C2B3A4-F5E6-7890-BCDE-F12345670088");

    private const string SubscriptionName = "sub-test-lb";
    private const string ResourceGroupName = "rg-test-lb";

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

    private static LoadBalancerData MinimalLoadBalancerData() =>
        new()
        {
            Location = AzureLocation.WestEurope,
            Sku = new LoadBalancerSku
            {
                Name = LoadBalancerSkuName.Standard
            }
        };

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string lbName = "test-lb-create";

        // Act
        var createResult = await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, lbName, MinimalLoadBalancerData());

        var loadBalancer = createResult.Value;

        // Assert
        Assert.That(loadBalancer, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loadBalancer.Data.Name, Is.EqualTo(lbName));
            Assert.That(loadBalancer.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Network/loadBalancers")));
            Assert.That(loadBalancer.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
        });
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsRetrievedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string lbName = "test-lb-get";

        await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, lbName, MinimalLoadBalancerData());

        // Act
        var loadBalancer = await resourceGroup.Value.GetLoadBalancerAsync(lbName);

        // Assert
        Assert.That(loadBalancer.Value.Data.Name, Is.EqualTo(lbName));
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string lbName = "test-lb-delete";

        await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, lbName, MinimalLoadBalancerData());

        // Act
        var loadBalancer = resourceGroup.Value.GetLoadBalancer(lbName);
        await loadBalancer.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetLoadBalancerAsync(lbName));
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerTagsAreUpdatedUsingSDK_TagsShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string lbName = "test-lb-update";

        var originalLb = await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, lbName, MinimalLoadBalancerData());

        // Act - Re-create with tags
        var lbDataWithTags = MinimalLoadBalancerData();
        lbDataWithTags.Tags.Add("env", "test");
        lbDataWithTags.Tags.Add("team", "platform");

        var updateResult = await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, lbName, lbDataWithTags);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.Value.Data.Tags.ContainsKey("env"), Is.True);
            Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("test"));
            Assert.That(updateResult.Value.Data.Tags["team"], Is.EqualTo("platform"));
        });
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancersAreListedByResourceGroupUsingSDK_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-lb-list-a", MinimalLoadBalancerData());
        await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-lb-list-b", MinimalLoadBalancerData());

        // Act
        var loadBalancers = resourceGroup.Value.GetLoadBalancers().GetAll().ToList();

        // Assert
        var names = loadBalancers.Select(lb => lb.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-lb-list-a"));
            Assert.That(names, Does.Contain("test-lb-list-b"));
        });
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancersAreListedBySubscriptionUsingSDK_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetLoadBalancers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-lb-sub-a", MinimalLoadBalancerData());

        // Act
        var loadBalancers = subscription.GetLoadBalancers().ToList();

        // Assert
        var names = loadBalancers.Select(lb => lb.Data.Name).ToList();
        Assert.That(names, Does.Contain("test-lb-sub-a"));
    }
}
