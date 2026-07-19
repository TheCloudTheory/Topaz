using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.Host.AMQP;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Filtering;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class ServiceBusMessageExpiryTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private const string SubscriptionName = "sub-expiry-test";
    private const string ResourceGroupName = "rg-expiry-test";
    private const string NamespaceName = "sb-expiry-test";
    private const string QueueWithDlq = "queue-with-dlq";
    private const string QueueWithoutDlq = "queue-without-dlq";

    private static readonly string ConnectionString =
        TopazResourceHelpers.GetServiceBusConnectionString(NamespaceName);
    private static readonly string ManagementConnectionString =
        TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName);

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var admin = new ServiceBusAdministrationClient(ManagementConnectionString);
        await admin.CreateQueueAsync(new CreateQueueOptions(QueueWithDlq) { DeadLetteringOnMessageExpiration = true });
        await admin.CreateQueueAsync(new CreateQueueOptions(QueueWithoutDlq) { DeadLetteringOnMessageExpiration = false });
    }

    [Test]
    public async Task ServiceBusMessageExpiry_WhenDeadLetteringEnabled_ExpiredMessageMovedToDlq()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(QueueWithDlq);

        // Send a message with a per-message TTL of 1 ms so it expires immediately.
        var message = new ServiceBusMessage("expire-me") { TimeToLive = TimeSpan.FromMilliseconds(1) };
        await sender.SendMessageAsync(message);

        // Wait for the TTL to elapse.
        await Task.Delay(50);

        RunScheduler();

        // The main queue should now be empty and the DLQ should have 1 message.
        await using var dlqReceiver = client.CreateReceiver(
            QueueWithDlq,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        Assert.That(dlqMessage, Is.Not.Null);
        Assert.That(dlqMessage.DeadLetterReason, Is.EqualTo("TTLExpiredException"));

        await using var mainReceiver = client.CreateReceiver(QueueWithDlq);
        var mainMessage = await mainReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500));
        Assert.That(mainMessage, Is.Null, "Main queue should be empty after expiry");
    }

    [Test]
    public async Task ServiceBusMessageExpiry_WhenDeadLetteringDisabled_ExpiredMessageDiscarded()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(QueueWithoutDlq);

        var message = new ServiceBusMessage("discard-me") { TimeToLive = TimeSpan.FromMilliseconds(1) };
        await sender.SendMessageAsync(message);

        await Task.Delay(50);

        RunScheduler();

        // Both main queue and DLQ should be empty.
        await using var mainReceiver = client.CreateReceiver(QueueWithoutDlq);
        var mainMessage = await mainReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500));
        Assert.That(mainMessage, Is.Null, "Main queue should be empty after expiry");

        await using var dlqReceiver = client.CreateReceiver(
            QueueWithoutDlq,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500));
        Assert.That(dlqMessage, Is.Null, "DLQ should be empty when deadLetteringOnMessageExpiration=false");
    }

    [Test]
    public async Task ServiceBusMessageExpiry_WhenEntityTtlElapsed_ExpiredMessageMovedToDlq()
    {
        // Re-create the queue with a very short entity-level DefaultMessageTimeToLive.
        var admin = new ServiceBusAdministrationClient(ManagementConnectionString);
        const string shortTtlQueue = "queue-short-entity-ttl";
        await admin.CreateQueueAsync(new CreateQueueOptions(shortTtlQueue)
        {
            DeadLetteringOnMessageExpiration = true,
            DefaultMessageTimeToLive = TimeSpan.FromSeconds(1)
        });

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(shortTtlQueue);

        // Send without per-message TTL — entity TTL applies.
        await sender.SendMessageAsync(new ServiceBusMessage("entity-ttl-expire-me"));

        await Task.Delay(TimeSpan.FromSeconds(2));

        RunScheduler();

        await using var dlqReceiver = client.CreateReceiver(
            shortTtlQueue,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        var dlqMessage = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        Assert.That(dlqMessage, Is.Not.Null);
        Assert.That(dlqMessage.DeadLetterReason, Is.EqualTo("TTLExpiredException"));
    }

    [Test]
    public async Task ServiceBusMessageExpiry_WhenTtlNotElapsed_MessageNotExpired()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(QueueWithDlq);

        // Long TTL — should survive the scan.
        var message = new ServiceBusMessage("keep-me") { TimeToLive = TimeSpan.FromHours(1) };
        await sender.SendMessageAsync(message);

        RunScheduler();

        await using var receiver = client.CreateReceiver(QueueWithDlq);
        var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        Assert.That(received, Is.Not.Null);
        Assert.That(received.Body.ToString(), Is.EqualTo("keep-me"));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void RunScheduler()
    {
        var logger = new PrettyTopazLogger();
        var ruleLoader = new ServiceBusRuleLoader(GlobalSettings.MainEmulatorDirectory);
        var scheduler = new ServiceBusMessageExpiryScheduler(ruleLoader, logger, TimeSpan.FromSeconds(30));
        scheduler.ScanAndExpire();
    }
}
