using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Topaz.Identity;

namespace Topaz.Tests.E2E;

public class EventHubTests
{
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
        
        await Program.Main([
            "eventhubs",
            "eventhub",
            "delete",
            "--name",
            "test",
            "--namespace-name",
            "test"
        ]);
        
        await Program.Main([
            "eventhubs",
            "eventhub",
            "create",
            "--name",
            "test",
            "--namespace-name",
            "test"
        ]);
    }
    
    [Test]
    public async Task EventHubTests_WhenMessageIsSent_ItShouldBeAvailableInEventHub()
    {
        // Arrange
        var producer = new EventHubProducerClient(
            "Endpoint=sb://localhost:8888;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            "eh-test");
        
        // Act
        await producer.SendAsync([
            new EventData(Encoding.UTF8.GetBytes("Hello World"))
        ]);        
    }
}