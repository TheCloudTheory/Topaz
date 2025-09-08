using System.Text;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Topaz.CLI;
using Topaz.Identity;

namespace Topaz.Tests.E2E;

public class EventHubTests
{
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
            "--subscription-id",
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
            "Endpoint=sb://topaz.eventhub.local.dev:8888;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            "eh-test");
        
        await Program.Main([
            "storage",
            "account",
            "delete",
            "--name",
            "test"
        ]);

        await Program.Main([
            "storage",
            "account",
            "create",
            "--name",
            "test",
            "-g",
            "test",
            "--location",
            "westeurope",
            "--subscription-id",
            Guid.Empty.ToString()
        ]);
        
        await Program.Main([
            "storage",
            "container",
            "create",
            "--name",
            "test",
            "--account-name",
            "test"
        ]);
        
        var storageClient = new BlobContainerClient(
            "DefaultEndpointsProtocol=http;AccountName=test;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://topaz.storage.blob.local.dev:8891/test;QueueEndpoint=http://topaz.storage.queue.local.dev:8899/test;TableEndpoint=http://topaz.storage.table.local.dev:8890/test;",
            "test");

        var processor = new EventProcessorClient(storageClient, EventHubConsumerClient.DefaultConsumerGroupName,
            "Endpoint=sb://topaz.eventhub.local.dev:8888;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            "eh-test");
        
        var receivedEvents = new List<EventData>();

        processor.ProcessEventAsync += e =>
        {
            receivedEvents.Add(e.Data);
            return Task.CompletedTask;
        };

        processor.ProcessErrorAsync += args => Task.CompletedTask;

        await processor.StartProcessingAsync();
        
        // Act
        await producer.SendAsync([
            new EventData("Hello World"u8.ToArray()),
        ]);     
        
        await producer.SendAsync([
            new EventData("Hello World 2"u8.ToArray()),
        ]); 
        
        await Task.Delay(TimeSpan.FromSeconds(5));
        await processor.StopProcessingAsync();
        
        // Assert
        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.That(receivedEvents, Has.Count.EqualTo(2));
    }
}