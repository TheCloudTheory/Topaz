using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Topaz.Identity;

namespace Topaz.Tests.E2E;

public class EventHubTests
{
    private static readonly ArmClientOptions armClientOptions = new ArmClientOptions
    {
        Environment = new ArmEnvironment(new Uri("https://localhost:8899"), "https://localhost:8899")
    };
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            Guid.Empty.ToString()
        ]);
        
        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            Guid.Empty.ToString(),
            "--name",
            "sub-test"
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            "test"
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            "test",
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString()
        ]);
        
        await Program.Main([
            "eventhubs",
            "namespace",
            "delete",
            "--name",
            "test"
        ]);
        
        await Program.Main([
            "eventhubs",
            "namespace",
            "create",
            "--name",
            "test",
            "-g",
            "rg-test",
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString(),
        ]);
    }
    
    [Test]
    public void EventHubTests_WhenNewHubIsRequested_ItShouldBeCreated()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, Guid.Empty.ToString(), armClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroup("test");
        var @namespace = resourceGroups.Value.GetEventHubsNamespace("test");

        // Act
        var hub = @namespace.Value.GetEventHubs().CreateOrUpdate(WaitUntil.Completed, "test-eh", new EventHubData());

        // Assert
        Assert.That(hub.Value.Data.Name, Is.EqualTo("test-eh"));
    }
}