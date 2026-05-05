using NUnit.Framework;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateResourceGroupToolTests
{
    private const string NewResourceGroupName = "rg-mcp-create-test";

    [Test]
    public async Task CreateResourceGroup_ReturnsCorrectName()
    {
        var result = await CreateResourceGroupTool.CreateResourceGroup(
            McpTestFixture.SubscriptionId,
            NewResourceGroupName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(NewResourceGroupName));
    }

    [Test]
    public async Task CreateResourceGroup_ReturnsNormalisedLocation()
    {
        var result = await CreateResourceGroupTool.CreateResourceGroup(
            McpTestFixture.SubscriptionId,
            NewResourceGroupName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Location, Is.EqualTo("westeurope"));
    }

    [Test]
    public async Task CreateResourceGroup_ReturnsSubscriptionId()
    {
        var result = await CreateResourceGroupTool.CreateResourceGroup(
            McpTestFixture.SubscriptionId,
            NewResourceGroupName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.SubscriptionId, Is.EqualTo(McpTestFixture.SubscriptionId.ToString()));
    }

    [Test]
    public async Task CreateResourceGroup_IsIdempotent()
    {
        // Creating twice must not throw.
        await CreateResourceGroupTool.CreateResourceGroup(
            McpTestFixture.SubscriptionId,
            NewResourceGroupName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.DoesNotThrowAsync(async () =>
            await CreateResourceGroupTool.CreateResourceGroup(
                McpTestFixture.SubscriptionId,
                NewResourceGroupName,
                "westeurope",
                McpTestFixture.ObjectId));
    }
}
