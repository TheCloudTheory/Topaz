using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class EventGridTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("4A1B2C3D-EEEE-4F5A-8BB1-3CFE44084F82");
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "test-namespace";
    private const string TopicName = "test-topic";
    private const string EventSubscriptionName = "test-subscription";

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
            "sub-test"
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName
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
            "eventgrid",
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
            "eventgrid",
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
            "eventgrid",
            "topic",
            "delete",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "eventgrid",
            "topic",
            "create",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "delete",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "create",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--endpoint-url",
            "https://example.com/webhook"
        ]);
    }

    [Test]
    public void EventGridTests_WhenNewNamespaceIsRequested_ItShouldBeCreated()
    {
        var namespacePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-namespace", NamespaceName, "metadata.json");

        Assert.That(File.Exists(namespacePath), Is.True);
    }

    [Test]
    public async Task EventGridTests_WhenExistingNamespaceIsDeleted_ItShouldBeDeleted()
    {
        var namespacePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-namespace ", NamespaceName, "metadata.json");

        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "delete",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(namespacePath), Is.False);
            Assert.That(code, Is.Zero);
        }
    }

    [Test]
    public async Task EventGridTests_WhenExistingNamespaceIsRequested_ItShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "show",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespacesInResourceGroupAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "list-resource-group",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespacesInSubscriptionAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "list-subscription",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenExistingNamespaceIsUpdated_ItShouldBeUpdated()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "update",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--sku-name",
            "Standard"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespaceKeysAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "list-keys",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespaceKeyIsRegenerated_ItShouldSucceed()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "regenerate-key",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--key-name",
            "key1"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public void EventGridTests_WhenNewTopicIsRequested_ItShouldBeCreated()
    {
        var topicPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-topic", TopicName, "metadata.json");

        Assert.That(File.Exists(topicPath), Is.True);
    }

    [Test]
    public async Task EventGridTests_WhenExistingTopicIsDeleted_ItShouldBeDeleted()
    {
        var topicPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-topic", TopicName, "metadata.json");

        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "delete",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(topicPath), Is.False);
            Assert.That(code, Is.Zero);
        }
    }

    [Test]
    public async Task EventGridTests_WhenExistingTopicIsRequested_ItShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "show",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicsInResourceGroupAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "list-resource-group",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicsInSubscriptionAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "list-subscription",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenExistingTopicIsUpdated_ItShouldBeUpdated()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "update",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--public-network-access",
            "Disabled"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicEventTypesAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "list-event-types",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicKeysAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "list-keys",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicKeyIsRegenerated_ItShouldSucceed()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "regenerate-key",
            "--name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--key-name",
            "key1"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public void EventGridTests_WhenNewTopicSubscriptionIsRequested_ItShouldBeCreated()
    {
        var eventSubscriptionPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-topic", TopicName,
            "topiceventsubscriptions", EventSubscriptionName, "metadata.json");

        Assert.That(File.Exists(eventSubscriptionPath), Is.True);
    }

    [Test]
    public async Task EventGridTests_WhenExistingTopicSubscriptionIsRequested_ItShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "show",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicSubscriptionsAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "list",
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenExistingTopicSubscriptionIsUpdated_ItShouldBeUpdated()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "update",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--endpoint-url",
            "https://example.com/other-webhook"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicSubscriptionUrlIsRequested_ItShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "show-endpoint-url",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenTopicSubscriptionDeliveryAttributesAreRequested_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "show-delivery-attributes",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenExistingTopicSubscriptionIsDeleted_ItShouldBeDeleted()
    {
        var eventSubscriptionPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-topic", TopicName,
            "topiceventsubscriptions", EventSubscriptionName, "metadata.json");

        var code = await Program.RunAsync([
            "eventgrid",
            "topic",
            "subscription",
            "delete",
            "--name",
            EventSubscriptionName,
            "--topic-name",
            TopicName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(eventSubscriptionPath), Is.False);
            Assert.That(code, Is.Zero);
        }
    }
}
