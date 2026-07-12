using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusSessionTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("F1A2B3C4-D5E6-7890-ABCD-EF1234567890");

    private const string SubscriptionName = "sub-session-test";
    private const string ResourceGroupName = "rg-session-test";
    private const string NamespaceName = "sb-session-test";
    private const string SessionQueueName = "session-queue";
    private const string NonSessionQueueName = "non-session-queue";

    private static readonly string ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(NamespaceName);
    private static readonly string ManagementConnectionString = TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName);

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var adminClient = new ServiceBusAdministrationClient(ManagementConnectionString);
        await adminClient.CreateQueueAsync(new CreateQueueOptions(SessionQueueName) { RequiresSession = true });
        await adminClient.CreateQueueAsync(new CreateQueueOptions(NonSessionQueueName) { RequiresSession = false });
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenMessageWithSessionIdIsSent_ItIsReceivedBySessionReceiver()
    {
        const string sessionId = "session-A";

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(SessionQueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("hello") { SessionId = sessionId });

        await using var sessionReceiver = await client.AcceptSessionAsync(SessionQueueName, sessionId);
        var msg = await sessionReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

        Assert.That(msg, Is.Not.Null);
        Assert.That(msg!.Body.ToString(), Is.EqualTo("hello"));
        Assert.That(msg.SessionId, Is.EqualTo(sessionId));
        await sessionReceiver.CompleteMessageAsync(msg);
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenSessionIsAlreadyLocked_SecondReceiverThrows()
    {
        const string sessionId = "session-locked";

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(SessionQueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("locked") { SessionId = sessionId });

        await using var first = await client.AcceptSessionAsync(SessionQueueName, sessionId);

        // A second receiver for the same session should fail with SessionCannotBeLocked
        Assert.ThrowsAsync<ServiceBusException>(
            async () => await client.AcceptSessionAsync(SessionQueueName, sessionId, new ServiceBusSessionReceiverOptions(), CancellationToken.None),
            "Expected SessionCannotBeLocked error"
        );

        await first.CloseAsync();
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenWildcardSessionReceiverIsUsed_ItPicksAvailableSession()
    {
        const string sessionId = "session-wild";

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(SessionQueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("wildcard") { SessionId = sessionId });

        // AcceptNextSessionAsync uses wildcard / null session filter
        await using var receiver = await client.AcceptNextSessionAsync(SessionQueueName);

        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

        Assert.That(msg, Is.Not.Null);
        Assert.That(msg!.Body.ToString(), Is.EqualTo("wildcard"));
        await receiver.CompleteMessageAsync(msg);
    }

    [Test]
    public async Task ServiceBusSessionTests_GetAndSetSessionState_RoundTrips()
    {
        const string sessionId = "session-state";
        var state = "my-state"u8.ToArray();

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(SessionQueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("state-test") { SessionId = sessionId });

        await using var receiver = await client.AcceptSessionAsync(SessionQueueName, sessionId);

        await receiver.SetSessionStateAsync(new BinaryData(state));
        var retrieved = await receiver.GetSessionStateAsync();

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ToArray(), Is.EqualTo(state));
    }

    [Test]
    public async Task ServiceBusSessionTests_RenewSessionLock_ReturnsFutureExpiry()
    {
        const string sessionId = "session-renew";

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(SessionQueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("renew") { SessionId = sessionId });

        await using var receiver = await client.AcceptSessionAsync(SessionQueueName, sessionId);

        var before = DateTimeOffset.UtcNow;
        await receiver.RenewSessionLockAsync();
        var expiry = receiver.SessionLockedUntil;

        Assert.That(expiry, Is.GreaterThan(before));
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenNonSessionReceiverAttachesToRequiresSessionQueue_ItThrows()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        // Non-session receiver on a requiresSession=true queue — Azure rejects with NotAllowed
        var receiver = client.CreateReceiver(SessionQueueName);

        var ex = Assert.ThrowsAsync<ServiceBusException>(
            async () => await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5)));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenMessageWithoutSessionIdSentToRequiresSessionQueue_MessageIsDiscarded()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(SessionQueueName);

        // Sending a message without a SessionId to a requiresSession queue should silently discard it
        await sender.SendMessageAsync(new ServiceBusMessage("no-session-id"));

        // Wildcard receiver should find nothing
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Assert.CatchAsync<OperationCanceledException>(
            async () => await client.AcceptNextSessionAsync(SessionQueueName, cancellationToken: cts.Token));
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenSessionMessageIsDeadLettered_SessionFilteredDlqReceiverGetsIt()
    {
        const string sessionId = "session-dlq";
        var adminClient = new ServiceBusAdministrationClient(ManagementConnectionString);
        await adminClient.CreateQueueAsync(new CreateQueueOptions("dlq-session-queue") { RequiresSession = true, MaxDeliveryCount = 10 });

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender("dlq-session-queue");
        await sender.SendMessageAsync(new ServiceBusMessage("dlq-body") { SessionId = sessionId });

        // Receive and explicitly dead-letter
        await using var receiver = await client.AcceptSessionAsync("dlq-session-queue", sessionId);
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
        Assert.That(msg, Is.Not.Null);
        await receiver.DeadLetterMessageAsync(msg!, "TestReason", "TestDescription");

        // Session-filtered receiver on DLQ should get the message
        await using var dlqReceiver = await client.AcceptSessionAsync(
            "dlq-session-queue", sessionId,
            new ServiceBusSessionReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var dlqMsg = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

        Assert.That(dlqMsg, Is.Not.Null);
        Assert.That(dlqMsg!.Body.ToString(), Is.EqualTo("dlq-body"));
        Assert.That(dlqMsg.SessionId, Is.EqualTo(sessionId));
        Assert.That(dlqMsg.DeadLetterReason, Is.EqualTo("TestReason"));
    }

    [Test]
    public async Task ServiceBusSessionTests_WhenSessionMessageExceedsMaxDeliveryCount_SessionFilteredDlqReceiverGetsIt()
    {
        const string sessionId = "session-maxdelivery";
        var adminClient = new ServiceBusAdministrationClient(ManagementConnectionString);
        await adminClient.CreateQueueAsync(new CreateQueueOptions("dlq-maxdelivery-queue") { RequiresSession = true, MaxDeliveryCount = 2 });

        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender("dlq-maxdelivery-queue");
        await sender.SendMessageAsync(new ServiceBusMessage("max-delivery-body") { SessionId = sessionId });

        // Abandon until maxDeliveryCount is exceeded
        for (var i = 0; i < 2; i++)
        {
            await using var receiver = await client.AcceptSessionAsync("dlq-maxdelivery-queue", sessionId);
            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
            Assert.That(msg, Is.Not.Null);
            await receiver.AbandonMessageAsync(msg!);
        }

        // Message should now be in DLQ with session preserved
        await using var dlqReceiver = await client.AcceptSessionAsync(
            "dlq-maxdelivery-queue", sessionId,
            new ServiceBusSessionReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var dlqMsg = await dlqReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

        Assert.That(dlqMsg, Is.Not.Null);
        Assert.That(dlqMsg!.Body.ToString(), Is.EqualTo("max-delivery-body"));
        Assert.That(dlqMsg.SessionId, Is.EqualTo(sessionId));
        Assert.That(dlqMsg.DeadLetterReason, Is.EqualTo("MaxDeliveryCountExceeded"));
    }
}
