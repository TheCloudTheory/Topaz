using Azure.Messaging.ServiceBus;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusTests
{
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "sb-test";
    private const string QueueName = "queue-test";
    
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly string ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString();
    
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
            "servicebus",
            "namespace",
            "delete",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName
        ]);
        
        await Program.Main([
            "servicebus",
            "namespace",
            "create",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName
        ]);
        
        await Program.Main([
            "servicebus",
            "queue",
            "delete",
            "--queue-name",
            QueueName,
            "--namespace-name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName
        ]);
        
        await Program.Main([
            "servicebus",
            "queue",
            "create",
            "--queue-name",
            QueueName,
            "--namespace-name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName
        ]);
    }

    [Test]
    public async Task ServiceBusTests_WhenMessageIsSentOntoQueue_ItShouldBeReceived()
    {
        // Arrange
        var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(QueueName);
        var message = new ServiceBusMessage("test message");
        var processorOptions = new ServiceBusProcessorOptions()
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
            MaxConcurrentCalls = 1
        };
        var processor = client.CreateProcessor(QueueName, processorOptions);
        var receivedMessage = new List<string>();
        
        processor.ProcessMessageAsync += args =>
        {
            receivedMessage.Add(args.Message.Body.ToString());
            return Task.CompletedTask;
        };

        processor.ProcessErrorAsync += args =>
        {
            Console.WriteLine($"Error when processing a message: {args.Exception.Message}");
            return Task.CompletedTask;
        };
        
        // Act
        try
        {
            await sender.SendMessageAsync(message);
            await processor.StartProcessingAsync();
            
            await Task.Delay(2000);
        }
        finally
        {
            await sender.DisposeAsync();
        }

        try
        {
            await processor.StopProcessingAsync();
        }
        finally
        {
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }

        // Assert
        Assert.That(receivedMessage, Is.Not.Empty);
        Assert.That(receivedMessage, Has.Count.EqualTo(1));
        Assert.That(receivedMessage[0], Is.EqualTo("test message"));
    }
}