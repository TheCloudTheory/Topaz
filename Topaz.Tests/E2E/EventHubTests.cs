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
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string EventHubNamespaceName = "test";
    private const string EventHubName = "test";
    private const string StorageAccountName = "devstoreaccount1";
    private const string ContainerName = "test";
    private static readonly string ConnectionString = TopazResourceHelpers.GetEventHubConnectionString();
    
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
            "eventhubs",
            "namespace",
            "delete",
            "--name",
            EventHubNamespaceName
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
            "--subscriptionId",
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
            EventHubNamespaceName
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
            EventHubNamespaceName
        ]);
        
        await Program.Main([
            "storage",
            "account",
            "delete",
            "--name",
            StorageAccountName
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
            StorageAccountName
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
    public async Task EventHubTests_WhenMessageIsSent_ItShouldBeAvailableInEventHub()
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
        
        // Act
        await producer.SendAsync([
            new EventData("Hello World"u8.ToArray()),
        ]);     
        
        await producer.SendAsync([
            new EventData("Hello World 2"u8.ToArray()),
        ]); 
        
        await processor.StartProcessingAsync();
        await Task.Delay(TimeSpan.FromSeconds(5));
        await processor.StopProcessingAsync();
        
        // Assert
        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.That(receivedEvents, Has.Count.EqualTo(2));
    }
}