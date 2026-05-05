using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateServiceBusToolTests
{
    private const string NamespaceName = "sb-mcp-create-test";
    private const string QueueName = "mcp-test-queue";
    private const string TopicName = "mcp-test-topic";

    [OneTimeSetUp]
    public async Task CreateNamespace()
    {
        // The namespace must exist before creating queues/topics.
        await CreateServiceBusTool.CreateServiceBusNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);
    }

    [Test]
    public async Task CreateServiceBusNamespace_ReturnsNamespaceName()
    {
        var result = await CreateServiceBusTool.CreateServiceBusNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.NamespaceName, Is.EqualTo(NamespaceName));
    }

    [Test]
    public async Task CreateServiceBusNamespace_ReturnsConnectionStringWithNamespace()
    {
        var result = await CreateServiceBusTool.CreateServiceBusNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.Multiple(() =>
        {
            Assert.That(result.ConnectionString, Does.Contain($"{NamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.DefaultServiceBusAmqpPort}"));
            Assert.That(result.ConnectionStringWithTls, Does.Contain($"{NamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort}"));
        });
    }

    [Test]
    public async Task CreateServiceBusQueue_ReturnsQueueDetails()
    {
        var result = await CreateServiceBusTool.CreateServiceBusQueue(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            QueueName,
            McpTestFixture.ObjectId);

        Assert.Multiple(() =>
        {
            Assert.That(result.EntityName, Is.EqualTo(QueueName));
            Assert.That(result.EntityType, Is.EqualTo("Queue"));
            Assert.That(result.NamespaceName, Is.EqualTo(NamespaceName));
        });
    }

    [Test]
    public async Task CreateServiceBusQueue_QueueIsReachableViaAdminClient()
    {
        await CreateServiceBusTool.CreateServiceBusQueue(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            QueueName,
            McpTestFixture.ObjectId);

        var adminClient = new ServiceBusAdministrationClient(
            TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));

        var exists = await adminClient.QueueExistsAsync(QueueName);
        Assert.That(exists.Value, Is.True);
    }

    [Test]
    public async Task CreateServiceBusTopic_ReturnsTopicDetails()
    {
        var result = await CreateServiceBusTool.CreateServiceBusTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            TopicName,
            McpTestFixture.ObjectId);

        Assert.Multiple(() =>
        {
            Assert.That(result.EntityName, Is.EqualTo(TopicName));
            Assert.That(result.EntityType, Is.EqualTo("Topic"));
            Assert.That(result.NamespaceName, Is.EqualTo(NamespaceName));
        });
    }

    [Test]
    public async Task CreateServiceBusTopic_TopicExistsViaArmSdk()
    {
        await CreateServiceBusTool.CreateServiceBusTopic(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            TopicName,
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(NamespaceName);
        var topic = await @namespace.Value.GetServiceBusTopicAsync(TopicName);

        Assert.That(topic.Value.Data.Name, Is.EqualTo(TopicName));
    }
}
