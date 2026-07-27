using NUnit.Framework;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateLogAnalyticsToolTests
{
    private const string WorkspaceName = "mcp-law-test";

    [Test]
    public async Task CreateLogAnalyticsWorkspace_ReturnsName()
    {
        var result = await CreateLogAnalyticsTool.CreateLogAnalyticsWorkspace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            WorkspaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(WorkspaceName));
    }

    [Test]
    public async Task CreateLogAnalyticsWorkspace_ReturnsCustomerId()
    {
        var result = await CreateLogAnalyticsTool.CreateLogAnalyticsWorkspace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            WorkspaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.CustomerId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateLogAnalyticsWorkspace_ReturnsRetentionInDays()
    {
        var result = await CreateLogAnalyticsTool.CreateLogAnalyticsWorkspace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            WorkspaceName,
            "westeurope",
            McpTestFixture.ObjectId,
            retentionInDays: 60);

        Assert.That(result.RetentionInDays, Is.EqualTo(60));
    }

    [Test]
    public async Task CreateLogAnalyticsWorkspace_ReturnsSucceededProvisioningState()
    {
        var result = await CreateLogAnalyticsTool.CreateLogAnalyticsWorkspace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            WorkspaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ProvisioningState, Is.EqualTo("Succeeded"));
    }

    [Test]
    public async Task CreateLogAnalyticsWorkspace_ReturnsResourceId()
    {
        var result = await CreateLogAnalyticsTool.CreateLogAnalyticsWorkspace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            WorkspaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ResourceId, Does.Contain("Microsoft.OperationalInsights/workspaces"));
    }
}
