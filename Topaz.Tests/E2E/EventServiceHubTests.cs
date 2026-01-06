using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class EventServiceHubTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("60AD9D95-F7AD-4BD9-AFB7-FA86DFB4B1D9");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string EventHubNamespaceName = "ns-test";
    
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
        
        await Program.Main([
            "eventhubs",
            "namespace",
            "delete",
            "--name",
            EventHubNamespaceName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        await Program.Main([
            "eventhubs",
            "namespace",
            "create",
            "--name",
            EventHubNamespaceName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }
    
    [Test]
    public async Task EventHubTests_WhenNewNamespaceIsRequested_ItShouldBeCreated()
    {
        // Arrange
        const string namespaceName = "eh-ns-test";
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        // Act
        var result = await resourceGroup.Value.GetEventHubsNamespaces().CreateOrUpdateAsync(WaitUntil.Completed,
            namespaceName, new EventHubsNamespaceData(AzureLocation.WestEurope));
        var response = await resourceGroup.Value.GetEventHubsNamespaceAsync(namespaceName);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(namespaceName));
            Assert.That(response.Value.Data.Name, Is.EqualTo(namespaceName));
        });
        
        // Cleanup
        await response.Value.DeleteAsync(WaitUntil.Completed);
    }
    
    [Test]
    public async Task EventHubTests_WhenNamespaceExistsAndIsCreatedOrUpdated_ItShouldBeUpdated()
    {
        // Arrange
        const string namespaceName = "eh-ns-update-test";
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        _ = await resourceGroup.Value.GetEventHubsNamespaces().CreateOrUpdateAsync(WaitUntil.Completed,
            namespaceName, new EventHubsNamespaceData(AzureLocation.WestEurope));

        // Act
        var result = await resourceGroup.Value.GetEventHubsNamespaces().CreateOrUpdateAsync(WaitUntil.Completed,
            namespaceName, new EventHubsNamespaceData(AzureLocation.WestEurope)
            {
                IsAutoInflateEnabled =  true,
                DisableLocalAuth = true
            });
        var response = await resourceGroup.Value.GetEventHubsNamespaceAsync(namespaceName);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(namespaceName));
            Assert.That(response.Value.Data.Name, Is.EqualTo(namespaceName));
            Assert.That(response.Value.Data.IsAutoInflateEnabled, Is.True);
            Assert.That(response.Value.Data.DisableLocalAuth, Is.True);
        });
        
        // Cleanup
        await response.Value.DeleteAsync(WaitUntil.Completed);
    }

    [Test]
    public async Task EventHubTests_WhenNewHubIsRequested_ItShouldBeCreated()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var @namespace = await resourceGroup.Value.GetEventHubsNamespaceAsync(EventHubNamespaceName);

        // Act
        var hub = await @namespace.Value.GetEventHubs().CreateOrUpdateAsync(WaitUntil.Completed, "test-eh", new EventHubData());

        // Assert
        Assert.That(hub.Value.Data.Name, Is.EqualTo("test-eh"));
        
        // Cleanup
        await hub.Value.DeleteAsync(WaitUntil.Completed);
    }
}