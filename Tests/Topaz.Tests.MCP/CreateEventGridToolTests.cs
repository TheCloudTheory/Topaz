using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateEventGridToolTests
{
    private const string TopicName = "eg-mcp-create-test";
    private const string SubscriptionName = "mcp-test-subscription";

    [OneTimeSetUp]
    public async Task CreateTopic()
    {
        await CreateEventGridTool.CreateEventGridTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            "westeurope",
            McpTestFixture.ObjectId);
    }

    [Test]
    public async Task CreateEventGridTopic_ReturnsTopicName()
    {
        var result = await CreateEventGridTool.CreateEventGridTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(TopicName));
    }

    [Test]
    public async Task CreateEventGridTopic_ReturnsEndpoint()
    {
        var result = await CreateEventGridTool.CreateEventGridTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Endpoint, Does.Contain(TopicName));
    }

    [Test]
    public async Task CreateEventGridTopic_ReturnsSharedAccessKeys()
    {
        var result = await CreateEventGridTool.CreateEventGridTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Key1, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Key2, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateEventGridTopic_TopicExistsViaArmSdk()
    {
        await CreateEventGridTool.CreateEventGridTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            "westeurope",
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var topic = await rg.Value.GetEventGridTopicAsync(TopicName);

        Assert.That(topic.Value.Data.Name, Is.EqualTo(TopicName));
    }

    [Test]
    public async Task CreateEventGridSubscriptionForWebhook_ReturnsSubscriptionName()
    {
        var result = await CreateEventGridTool.CreateEventGridSubscriptionForWebhook(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            SubscriptionName,
            "westeurope",
            McpTestFixture.ObjectId,
            "https://example.com/webhook");

        Assert.That(result.Name, Is.EqualTo(SubscriptionName));
    }

    [Test]
    public async Task CreateEventGridSubscriptionForWebhook_ReturnsEndpoint()
    {
        var result = await CreateEventGridTool.CreateEventGridSubscriptionForWebhook(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            SubscriptionName,
            "westeurope",
            McpTestFixture.ObjectId,
            "https://example.com/webhook");

        Assert.That(result.Endpoint, Does.Contain(TopicName));
    }

    [Test]
    public async Task CreateEventGridSubscriptionForWebhook_SubscriptionExistsViaArmSdk()
    {
        await CreateEventGridTool.CreateEventGridSubscriptionForWebhook(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            TopicName,
            SubscriptionName,
            "westeurope",
            McpTestFixture.ObjectId,
            "https://example.com/webhook");

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var topic = await rg.Value.GetEventGridTopicAsync(TopicName);
        var eventSubscription = await topic.Value.GetTopicEventSubscriptionAsync(SubscriptionName);

        Assert.That(eventSubscription.Value.Data.Name, Is.EqualTo(SubscriptionName));
    }
}
