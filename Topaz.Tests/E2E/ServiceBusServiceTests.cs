using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusServiceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "sb-test";
    
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
            ResourceGroupName
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
            "servicebus",
            "namespace",
            "delete",
            "--name",
            NamespaceName
        ]);
    }

    [Test]
    public async Task ServiceBusServiceTests_WhenNamespaceIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, Guid.Empty.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        
        // Assert
        Assert.That(@namespace, Is.Not.Null);
        Assert.That(@namespace.Value, Is.Not.Null);
        Assert.That(@namespace.Value.Data.Name, Is.EqualTo(NamespaceName));
    }
}