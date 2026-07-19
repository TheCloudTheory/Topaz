using Azure;
using Azure.Messaging.ServiceBus;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// E2E tests for Service Bus ForwardDeadLetteredMessagesTo semantics.
/// Covers: queue forwarding, topic-subscription forwarding, and fallback to local DLQ
/// when the target entity does not exist.
/// </summary>
public class ServiceBusForwardDeadLetteredMessagesToTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("C3F52A1D-8E64-4B3F-AA9B-7E53FB41D6C2");

    private const string ResourceGroupName = "test-fdlmt";
    private const string NamespaceName = "sb-fdlmt-test";
    private const string SourceQueueName = "fdlmt-source";
    private const string TargetQueueName = "fdlmt-target";
    private const string MissingTargetQueueName = "fdlmt-missing-target";
    private const string TopicName = "fdlmt-topic";
    private const string SubscriptionName = "fdlmt-sub";

    private static readonly string ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(NamespaceName);
    private static readonly string ManagementConnectionString =
        TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName);

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", "sub-fdlmt"]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var sbNamespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);

        // Target queue (plain, no forwarding)
        await sbNamespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, TargetQueueName, new ServiceBusQueueData());

        // Source queue: forwards DLQ messages to target, MaxDeliveryCount=1 so one abandon triggers DLQ
        await sbNamespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, SourceQueueName, new ServiceBusQueueData
            {
                ForwardDeadLetteredMessagesTo = TargetQueueName,
                MaxDeliveryCount = 1
            });

        // Queue with a forwarding target that does not exist
        await sbNamespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, MissingTargetQueueName, new ServiceBusQueueData
            {
                ForwardDeadLetteredMessagesTo = "nonexistent-queue",
                MaxDeliveryCount = 1
            });

        // Topic + subscription with ForwardDeadLetteredMessagesTo via ARM SDK
        await sbNamespace.Value.GetServiceBusTopics()
            .CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new ServiceBusTopicData());
        var topic = await sbNamespace.Value.GetServiceBusTopics().GetAsync(TopicName);
        await topic.Value.GetServiceBusSubscriptions()
            .CreateOrUpdateAsync(WaitUntil.Completed, SubscriptionName, new ServiceBusSubscriptionData
            {
                ForwardDeadLetteredMessagesTo = TargetQueueName,
                MaxDeliveryCount = 1
            });
    }

    [Test]
    public async Task Queue_ForwardDeadLetteredMessagesTo_MessageAppearsInTargetQueue()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(SourceQueueName);
        var receiver = client.CreateReceiver(SourceQueueName);
        var targetReceiver = client.CreateReceiver(TargetQueueName);
        var sourceDlqReceiver = client.CreateReceiver(SourceQueueName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("forward-dlq-queue"));

        // Abandon once — MaxDeliveryCount=1 triggers auto-DLQ → forwarded to target
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(msg, Is.Not.Null, "Expected to receive the message on source queue.");
        await receiver.AbandonMessageAsync(msg!);

        // Message must arrive in the target queue, not in source/$deadletterqueue
        var targetMsg = await targetReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(targetMsg, Is.Not.Null, "Expected forwarded message in target queue.");
        Assert.That(targetMsg!.Body.ToString(), Is.EqualTo("forward-dlq-queue"));
        await targetReceiver.CompleteMessageAsync(targetMsg);

        var sourceDlqMsg = await sourceDlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(sourceDlqMsg, Is.Null, "Source DLQ should be empty — message was forwarded.");
    }

    [Test]
    public async Task Queue_ForwardDeadLetteredMessagesTo_ExplicitDeadLetter_MessageAppearsInTargetQueue()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(SourceQueueName);
        var receiver = client.CreateReceiver(SourceQueueName);
        var targetReceiver = client.CreateReceiver(TargetQueueName);
        var sourceDlqReceiver = client.CreateReceiver(SourceQueueName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("explicit-dlq-forward"));

        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(msg, Is.Not.Null);
        await receiver.DeadLetterMessageAsync(msg!, "TestReason", "TestDescription");

        var targetMsg = await targetReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(targetMsg, Is.Not.Null, "Expected forwarded message in target queue after explicit dead-letter.");
        Assert.That(targetMsg!.Body.ToString(), Is.EqualTo("explicit-dlq-forward"));
        await targetReceiver.CompleteMessageAsync(targetMsg);

        var sourceDlqMsg = await sourceDlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(sourceDlqMsg, Is.Null, "Source DLQ should be empty.");
    }

    [Test]
    public async Task Subscription_ForwardDeadLetteredMessagesTo_MessageAppearsInTargetQueue()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(TopicName);
        var receiver = client.CreateReceiver(TopicName, SubscriptionName);
        var targetReceiver = client.CreateReceiver(TargetQueueName);
        var subDlqReceiver = client.CreateReceiver(TopicName, SubscriptionName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("forward-dlq-subscription"));

        // Abandon once — MaxDeliveryCount=1 triggers auto-DLQ → forwarded to target
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(msg, Is.Not.Null, "Expected to receive the message on subscription.");
        await receiver.AbandonMessageAsync(msg!);

        var targetMsg = await targetReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(targetMsg, Is.Not.Null, "Expected forwarded message in target queue.");
        Assert.That(targetMsg!.Body.ToString(), Is.EqualTo("forward-dlq-subscription"));
        await targetReceiver.CompleteMessageAsync(targetMsg);

        var subDlqMsg = await subDlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        Assert.That(subDlqMsg, Is.Null, "Subscription DLQ should be empty — message was forwarded.");
    }

    [Test]
    public async Task Queue_ForwardDeadLetteredMessagesTo_NonexistentTarget_FallsBackToLocalDlq()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var sender = client.CreateSender(MissingTargetQueueName);
        var receiver = client.CreateReceiver(MissingTargetQueueName);
        var dlqReceiver = client.CreateReceiver(MissingTargetQueueName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        await sender.SendMessageAsync(new ServiceBusMessage("fallback-to-dlq"));

        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(msg, Is.Not.Null, "Expected to receive the message.");
        await receiver.AbandonMessageAsync(msg!);

        // Target does not exist → message must land in the local DLQ
        var dlqMsg = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(dlqMsg, Is.Not.Null, "Expected message in local DLQ when forwarding target does not exist.");
        Assert.That(dlqMsg!.Body.ToString(), Is.EqualTo("fallback-to-dlq"));
        await dlqReceiver.CompleteMessageAsync(dlqMsg);
    }
}
