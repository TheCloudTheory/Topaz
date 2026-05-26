using Topaz.CLI;
using Azure.Messaging.ServiceBus;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusTests
{
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "sb-test";
    private const string QueueName = "queue-test";
    
    private static readonly Guid SubscriptionId = Guid.Parse("922E7F4C-A0BB-4D49-84B8-2530EA3033D2");
    private static readonly string ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(NamespaceName);
    private static readonly string ConnectionStringTls = TopazResourceHelpers.GetServiceBusConnectionStringWithTls(NamespaceName);
    
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
        
        await Program.RunAsync([
            "servicebus",
            "namespace",
            "delete",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "servicebus",
            "namespace",
            "create",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "servicebus",
            "queue",
            "delete",
            "--queue-name",
            QueueName,
            "--namespace-name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "servicebus",
            "queue",
            "create",
            "--queue-name",
            QueueName,
            "--namespace-name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task ServiceBusTests_WhenMessageIsSentOntoQueue_ItShouldBeReceived()
    {
        await RunTest(ConnectionString);
    }
    
    [Test]
    public async Task ServiceBusTests_WhenMessageIsSentOntoQueueUsingTls_ItShouldBeReceived()
    {
        await RunTest(ConnectionStringTls);
    }

    [Test]
    public async Task ServiceBusTests_WhenMultipleMessagesAreReceivedAndCompleted_ItShouldConsumeEveryMessage()
    {
        var expectedMessages = new[]
        {
            "test message 1",
            "test message 2",
            "test message 3"
        };

        var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(QueueName);
        var receiver = client.CreateReceiver(QueueName);
        var receivedMessages = new List<string>();

        try
        {
            foreach (var body in expectedMessages)
            {
                await sender.SendMessageAsync(new ServiceBusMessage(body));
            }

            foreach (var expectedBody in expectedMessages)
            {
                var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

                Assert.That(received, Is.Not.Null, $"Expected to receive '{expectedBody}'.");

                receivedMessages.Add(received!.Body.ToString());
                await receiver.CompleteMessageAsync(received);
            }

            var extraMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
            Assert.That(extraMessage, Is.Null);
        }
        finally
        {
            await sender.DisposeAsync();
            await receiver.DisposeAsync();
            await client.DisposeAsync();
        }

        Assert.That(receivedMessages, Is.EqualTo(expectedMessages));
    }

    private static async Task RunTest(string connectionString)
    {
        // Arrange
        var client = new ServiceBusClient(connectionString);
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