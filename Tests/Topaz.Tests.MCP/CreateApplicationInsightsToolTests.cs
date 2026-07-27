using NUnit.Framework;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateApplicationInsightsToolTests
{
    private const string ComponentName = "mcp-appinsights-test";

    [Test]
    public async Task CreateApplicationInsights_ReturnsName()
    {
        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            ComponentName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(ComponentName));
    }

    [Test]
    public async Task CreateApplicationInsights_ReturnsInstrumentationKey()
    {
        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            ComponentName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.InstrumentationKey, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateApplicationInsights_ReturnsConnectionString()
    {
        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            ComponentName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ConnectionString, Does.StartWith("InstrumentationKey="));
    }

    [Test]
    public async Task CreateApplicationInsights_ConnectionStringContainsIngestionEndpoint()
    {
        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            ComponentName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ConnectionString, Does.Contain($"{ComponentName}.applicationinsights.topaz.local.dev"));
    }

    [Test]
    public async Task CreateApplicationInsights_ReturnsSucceededProvisioningState()
    {
        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            ComponentName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ProvisioningState, Is.EqualTo("Succeeded"));
    }

    [Test]
    public async Task CreateApplicationInsights_ReturnsResourceId()
    {
        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            ComponentName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ResourceId, Does.Contain("microsoft.insights/components"));
    }

    [Test]
    public async Task CreateApplicationInsights_WithWorkspaceResourceId_ReturnsLinkedWorkspace()
    {
        var workspaceName = "mcp-law-for-insights";
        var workspace = await CreateLogAnalyticsTool.CreateLogAnalyticsWorkspace(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            workspaceName,
            "westeurope",
            McpTestFixture.ObjectId);

        var result = await CreateApplicationInsightsTool.CreateApplicationInsights(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            "mcp-appinsights-linked",
            "westeurope",
            McpTestFixture.ObjectId,
            workspaceResourceId: workspace.ResourceId);

        Assert.That(result.Name, Is.EqualTo("mcp-appinsights-linked"));
    }
}
