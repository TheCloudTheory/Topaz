using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// End-to-end tests for Service Bus topic subscription rule-based message filtering.
/// Each test publishes messages to a topic and asserts that only matching messages
/// are delivered to each subscription based on its active rule set.
/// </summary>
public class TopicSubscriptionRuleFilteringTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("F1A2B3C4-D5E6-7890-ABCD-EF1234567890");

    private const string SubscriptionName = "sub-filter-test";
    private const string ResourceGroupName = "rg-filter-test";
    private const string NamespaceName = "sb-filter-test";
    private const string TopicName = "filter-topic";

    private static readonly string ConnectionString =
        TopazResourceHelpers.GetServiceBusConnectionString(NamespaceName);

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var adminClient = new ServiceBusAdministrationClient(
            TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        await adminClient.CreateTopicAsync(TopicName);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private ServiceBusAdministrationClient AdminClient() =>
        new(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));

    private ServiceBusClient MessageClient() => new(ConnectionString);

    private static ServiceBusMessage MessageWithProperty(string body, string key, string value)
    {
        var msg = new ServiceBusMessage(body);
        msg.ApplicationProperties[key] = value;
        return msg;
    }

    private static async Task<List<ServiceBusReceivedMessage>> ReceiveAllAsync(
        ServiceBusReceiver receiver, int expected, TimeSpan? timeout = null)
    {
        var messages = new List<ServiceBusReceivedMessage>();
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        while (messages.Count < expected && DateTime.UtcNow < deadline)
        {
            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
            if (msg == null) break;
            messages.Add(msg);
            await receiver.CompleteMessageAsync(msg);
        }
        return messages;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Topic_TrueFilterRule_ForwardsAllMessages()
    {
        const string subName = "sub-true";
        var admin = AdminClient();
        await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subName));
        // Default $Default rule is TrueFilter — no rule replacement needed.

        await using var client = MessageClient();
        var sender = client.CreateSender(TopicName);
        var receiver = client.CreateReceiver(TopicName, subName);

        await sender.SendMessageAsync(new ServiceBusMessage("msg-a"));
        await sender.SendMessageAsync(new ServiceBusMessage("msg-b"));

        var received = await ReceiveAllAsync(receiver, 2);

        Assert.That(received, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Topic_CorrelationFilter_ForwardsMatchingMessage()
    {
        const string subName = "sub-corr";
        var admin = AdminClient();
        await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subName));

        // Replace the default TrueFilter with a CorrelationFilter on CorrelationId.
        await admin.DeleteRuleAsync(TopicName, subName, "$Default");
        await admin.CreateRuleAsync(TopicName, subName,
            new CreateRuleOptions("corr-filter",
                new CorrelationRuleFilter { CorrelationId = "important" }));

        await using var client = MessageClient();
        var sender = client.CreateSender(TopicName);
        var receiver = client.CreateReceiver(TopicName, subName);

        await sender.SendMessageAsync(new ServiceBusMessage("match") { CorrelationId = "important" });
        await sender.SendMessageAsync(new ServiceBusMessage("no-match") { CorrelationId = "other" });

        var received = await ReceiveAllAsync(receiver, 1);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Body.ToString(), Is.EqualTo("match"));
    }

    [Test]
    public async Task Topic_SqlFilter_ForwardsMatchingMessage()
    {
        const string subName = "sub-sql";
        var admin = AdminClient();
        await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subName));

        await admin.DeleteRuleAsync(TopicName, subName, "$Default");
        await admin.CreateRuleAsync(TopicName, subName,
            new CreateRuleOptions("color-filter",
                new SqlRuleFilter("color = 'red'")));

        await using var client = MessageClient();
        var sender = client.CreateSender(TopicName);
        var receiver = client.CreateReceiver(TopicName, subName);

        await sender.SendMessageAsync(MessageWithProperty("red-msg", "color", "red"));
        await sender.SendMessageAsync(MessageWithProperty("blue-msg", "color", "blue"));

        var received = await ReceiveAllAsync(receiver, 1);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Body.ToString(), Is.EqualTo("red-msg"));
    }

    [Test]
    public async Task Topic_SqlFilter_MultipleSubscriptions_ReceiveIndependently()
    {
        const string subRed = "sub-red";
        const string subBlue = "sub-blue";
        var admin = AdminClient();

        await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subRed));
        await admin.DeleteRuleAsync(TopicName, subRed, "$Default");
        await admin.CreateRuleAsync(TopicName, subRed,
            new CreateRuleOptions("red-only", new SqlRuleFilter("color = 'red'")));

        await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subBlue));
        await admin.DeleteRuleAsync(TopicName, subBlue, "$Default");
        await admin.CreateRuleAsync(TopicName, subBlue,
            new CreateRuleOptions("blue-only", new SqlRuleFilter("color = 'blue'")));

        await using var client = MessageClient();
        var sender = client.CreateSender(TopicName);
        var receiverRed  = client.CreateReceiver(TopicName, subRed);
        var receiverBlue = client.CreateReceiver(TopicName, subBlue);

        await sender.SendMessageAsync(MessageWithProperty("red-msg", "color", "red"));
        await sender.SendMessageAsync(MessageWithProperty("blue-msg", "color", "blue"));

        var redMessages  = await ReceiveAllAsync(receiverRed,  1);
        var blueMessages = await ReceiveAllAsync(receiverBlue, 1);

        Assert.Multiple(() =>
        {
            Assert.That(redMessages,  Has.Count.EqualTo(1));
            Assert.That(redMessages[0].Body.ToString(), Is.EqualTo("red-msg"));
            Assert.That(blueMessages, Has.Count.EqualTo(1));
            Assert.That(blueMessages[0].Body.ToString(), Is.EqualTo("blue-msg"));
        });
    }

    [Test]
    public async Task Topic_SqlRuleAction_MutatesPropertyOnMatchedMessage()
    {
        const string subName = "sub-action";
        var admin = AdminClient();
        await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subName));

        await admin.DeleteRuleAsync(TopicName, subName, "$Default");
        await admin.CreateRuleAsync(TopicName, subName,
            new CreateRuleOptions("add-processed", new SqlRuleFilter("1=1"))
            {
                Action = new SqlRuleAction("SET processed = 'true'")
            });

        await using var client = MessageClient();
        var sender = client.CreateSender(TopicName);
        var receiver = client.CreateReceiver(TopicName, subName);

        await sender.SendMessageAsync(new ServiceBusMessage("action-msg"));

        var received = await ReceiveAllAsync(receiver, 1);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].ApplicationProperties.ContainsKey("processed"), Is.True);
        Assert.That(received[0].ApplicationProperties["processed"]?.ToString(), Is.EqualTo("true"));
    }
}
