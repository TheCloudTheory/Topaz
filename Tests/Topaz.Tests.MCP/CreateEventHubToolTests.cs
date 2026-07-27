using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateEventHubToolTests
{
    private const string NamespaceName = "eh-mcp-create-test";
    private const string EventHubName = "mcp-test-hub";

    [OneTimeSetUp]
    public async Task CreateNamespace()
    {
        await CreateEventHubTool.CreateEventHubNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);
    }

    [Test]
    public async Task CreateEventHubNamespace_ReturnsNamespaceName()
    {
        var result = await CreateEventHubTool.CreateEventHubNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.NamespaceName, Is.EqualTo(NamespaceName));
    }

    [Test]
    public async Task CreateEventHubNamespace_ReturnsConnectionStringWithNamespace()
    {
        var result = await CreateEventHubTool.CreateEventHubNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ConnectionString, Does.Contain($"{NamespaceName}.eventhub.topaz.local.dev:{GlobalSettings.DefaultEventHubAmqpPort}"));
    }

    [Test]
    public async Task CreateEventHubNamespace_NamespaceExistsViaArmSdk()
    {
        await CreateEventHubTool.CreateEventHubNamespace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var @namespace = await rg.Value.GetEventHubsNamespaceAsync(NamespaceName);

        Assert.That(@namespace.Value.Data.Name, Is.EqualTo(NamespaceName));
    }

    [Test]
    public async Task CreateEventHub_ReturnsEventHubName()
    {
        var result = await CreateEventHubTool.CreateEventHub(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            EventHubName,
            McpTestFixture.ObjectId);

        Assert.That(result.EventHubName, Is.EqualTo(EventHubName));
    }

    [Test]
    public async Task CreateEventHub_ReturnsPartitionCount()
    {
        var result = await CreateEventHubTool.CreateEventHub(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            EventHubName,
            McpTestFixture.ObjectId,
            partitionCount: 2);

        Assert.That(result.PartitionCount, Is.EqualTo(2));
    }

    [Test]
    public async Task CreateEventHub_HubExistsViaArmSdk()
    {
        await CreateEventHubTool.CreateEventHub(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            NamespaceName,
            EventHubName,
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var @namespace = await rg.Value.GetEventHubsNamespaceAsync(NamespaceName);
        var hub = await @namespace.Value.GetEventHubAsync(EventHubName);

        Assert.That(hub.Value.Data.Name, Is.EqualTo(EventHubName));
    }
}
