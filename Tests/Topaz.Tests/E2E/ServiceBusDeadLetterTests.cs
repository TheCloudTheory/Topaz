using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// E2E tests for Service Bus dead-letter queue semantics.
/// Covers: explicit dead-letter via management API, auto-DLQ on MaxDeliveryCount,
/// Complete removes message, and topic-subscription DLQ.
/// </summary>
public class ServiceBusDeadLetterTests
{
    private const string SubscriptionName = "sub-dlq-test";
    private const string ResourceGroupName = "test-dlq";
    private const string NamespaceName = "sb-dlq-test";
    private const string QueueName = "dlq-queue";
    private const string TopicName = "dlq-topic";
    private const string TopicSubscriptionName = "dlq-sub";

    private static readonly Guid SubscriptionId = Guid.Parse("B2E41F3C-9D47-4A2E-BB8A-6D42EA30F5C1");
    private static readonly string ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(NamespaceName);

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "queue", "create", "--queue-name", QueueName, "--namespace-name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);

        var adminClient = new ServiceBusAdministrationClient(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        await adminClient.CreateTopicAsync(TopicName);
        await adminClient.CreateSubscriptionAsync(TopicName, TopicSubscriptionName);
    }

    [Test]
    public async Task DeadLetter_ExplicitDeadLetterAsync_MessageAppearsInDlq()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(QueueName);
        var receiver = client.CreateReceiver(QueueName);
        var dlqReceiver = client.CreateReceiver(QueueName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("dead-letter-me"));

        var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(received, Is.Not.Null, "Expected to receive the message.");

        await receiver.DeadLetterMessageAsync(received!, "TestReason", "TestDescription");

        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(dlqMessage, Is.Not.Null, "Expected message in dead-letter queue.");
        Assert.That(dlqMessage!.Body.ToString(), Is.EqualTo("dead-letter-me"));

        await dlqReceiver.CompleteMessageAsync(dlqMessage);

        var extraMainMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(extraMainMessage, Is.Null, "Main queue should be empty after dead-lettering.");
    }

    [Test]
    public async Task DeadLetter_MaxDeliveryCountExceeded_MessageAutoDeadLettered()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(QueueName);
        // MaxDeliveryCount is 3, so abandon 3 times to trigger auto-DLQ.
        var receiver = client.CreateReceiver(QueueName);
        var dlqReceiver = client.CreateReceiver(QueueName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("exceed-max-delivery"));

        // Abandon MaxDeliveryCount times; on the final abandon the broker should DLQ it.
        for (var i = 0; i < 10; i++)
        {
            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
            Assert.That(msg, Is.Not.Null, $"Expected to receive message on attempt {i + 1}.");
            await receiver.AbandonMessageAsync(msg!);
        }

        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(dlqMessage, Is.Not.Null, "Expected message in dead-letter queue after MaxDeliveryCount exceeded.");
        Assert.That(dlqMessage!.Body.ToString(), Is.EqualTo("exceed-max-delivery"));

        await dlqReceiver.CompleteMessageAsync(dlqMessage);

        var extraMainMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(extraMainMessage, Is.Null, "Main queue should be empty.");
    }

    [Test]
    public async Task DeadLetter_CompleteMessageAsync_MessageIsGone()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(QueueName);
        var receiver = client.CreateReceiver(QueueName);
        var dlqReceiver = client.CreateReceiver(QueueName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("complete-me"));

        var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(received, Is.Not.Null);

        await receiver.CompleteMessageAsync(received!);

        var extraMainMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(extraMainMessage, Is.Null, "Completed message should not be re-delivered.");

        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(dlqMessage, Is.Null, "Completed message should not appear in DLQ.");
    }

    [Test]
    public async Task DeadLetter_TopicSubscription_ExplicitDeadLetter_MessageAppearsInSubscriptionDlq()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(TopicName);
        var receiver = client.CreateReceiver(TopicName, TopicSubscriptionName);
        var dlqReceiver = client.CreateReceiver(TopicName, TopicSubscriptionName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("topic-dead-letter"));

        var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(received, Is.Not.Null, "Expected to receive message on subscription.");

        await receiver.DeadLetterMessageAsync(received!, "TopicTestReason", "TopicTestDescription");

        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(dlqMessage, Is.Not.Null, "Expected message in subscription dead-letter queue.");
        Assert.That(dlqMessage!.Body.ToString(), Is.EqualTo("topic-dead-letter"));

        await dlqReceiver.CompleteMessageAsync(dlqMessage);
    }
}
