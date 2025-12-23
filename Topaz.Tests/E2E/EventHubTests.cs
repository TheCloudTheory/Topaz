using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class EventHubTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("100E4CCB-7630-44A4-85D9-3ED9F8492DAC");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string EventHubNamespaceName = "test";
    private const string EventHubName = "test";
    private const string StorageAccountName = "devstoreaccount1";
    private const string ContainerName = "test";
    private static readonly string ConnectionString = TopazResourceHelpers.GetEventHubConnectionString(EventHubNamespaceName);
    
    private string _key = null!;
    
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
            SubscriptionId.ToString()
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
        
        await Program.Main([
            "eventhubs",
            "eventhub",
            "delete",
            "--name",
            EventHubName,
            "-g",
            ResourceGroupName,
            "--namespace-name",
            EventHubNamespaceName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "eventhubs",
            "eventhub",
            "create",
            "--name",
            EventHubName,
            "-g",
            ResourceGroupName,
            "--namespace-name",
            EventHubNamespaceName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "storage",
            "account",
            "delete",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.Main([
            "storage",
            "account",
            "create",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "storage",
            "container",
            "create",
            "--name",
            ContainerName,
            "--account-name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
        var keys = storageAccount.Value.GetKeys().ToArray();

        _key = keys[0].Value;
    }
    
    [Test]
    public async Task EventHubTests_WhenMessageIsSent_ItShouldBeAvailableInEventHub1()
    {
        // Arrange
        var producer = new EventHubProducerClient(
            ConnectionString,
            EventHubName);
        
        var storageClient = new BlobContainerClient(
            TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key),
            ContainerName);

        var processor = new EventProcessorClient(storageClient, EventHubConsumerClient.DefaultConsumerGroupName,
            ConnectionString,
            EventHubName);
        
        var receivedEvents = new List<EventData>();

        processor.ProcessEventAsync += e =>
        {
            receivedEvents.Add(e.Data);
            return Task.CompletedTask;
        };

        processor.ProcessErrorAsync += args =>
        {
            Console.WriteLine($"Error when processing a message: {args.Exception.Message}");
            return Task.CompletedTask;
        };

        processor.PartitionInitializingAsync += args =>
        {
            if (args.CancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            // If no checkpoint was found, start processing
            // events enqueued now or in the future.

            var startPositionWhenNoCheckpoint =
                EventPosition.FromEnqueuedTime(DateTimeOffset.UtcNow);

            args.DefaultStartingPosition = startPositionWhenNoCheckpoint;

            return Task.CompletedTask;
        };
        
        // Act
        try
        {
            await producer.SendAsync([
                new EventData("Hello World"u8.ToArray()),
            ]);     
        
            await producer.SendAsync([
                new EventData("Hello World 2"u8.ToArray()),
            ]); 
        }
        finally
        {
            await producer.DisposeAsync();
        }
        
        await processor.StartProcessingAsync();
        await Task.Delay(TimeSpan.FromSeconds(30));
        await processor.StopProcessingAsync();
        
        // Assert
        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.That(receivedEvents, Has.Count.EqualTo(2));
    }
}