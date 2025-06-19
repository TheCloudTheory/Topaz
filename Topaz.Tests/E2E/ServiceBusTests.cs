using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Topaz.CLI;

namespace Topaz.Tests.E2E;

public class ServiceBusTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    
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
    }

    [Test]
    public async Task ServiceBusTests_WhenMessageIsSentOntoQueue_ItShouldBeReceived()
    {
        // Arrange
        const string queueName = "test-queue";
        var client = new ServiceBusClient("Endpoint=sb://localhost:8887;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");
        var sender = client.CreateSender(queueName);
        var message = new ServiceBusMessage("test message");
        var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());
        var receivedMessage = new List<string>();
        
        // Act
        try
        {
            await sender.SendMessageAsync(message);
        }
        finally
        {
            await sender.DisposeAsync();
            
        }
        
        await Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            processor.ProcessMessageAsync += async args =>
            {
                receivedMessage.Add(args.Message.Body.ToString());
                await args.CompleteMessageAsync(args.Message);
            };
            
            await processor.StartProcessingAsync();
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