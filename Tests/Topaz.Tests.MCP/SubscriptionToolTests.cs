using NUnit.Framework;
using Topaz.CLI;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class SubscriptionToolTests
{
    private static readonly Guid ExtraSubscriptionId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    [OneTimeSetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "create",
            "--id", ExtraSubscriptionId.ToString(),
            "--name", "mcp-extra-subscription"]);
    }

    [Test]
    public async Task ListSubscriptions_ReturnsFixtureSubscription()
    {
        var subscriptions = await SubscriptionTool.ListSubscriptions(McpTestFixture.ObjectId);

        Assert.That(subscriptions, Has.Some.Matches<SubscriptionTool.Subscription>(
            s => s.SubscriptionId == McpTestFixture.SubscriptionId.ToString()));
    }

    [Test]
    public async Task ListSubscriptions_ReturnsSubscriptionCreatedViaCli()
    {
        var subscriptions = await SubscriptionTool.ListSubscriptions(McpTestFixture.ObjectId);

        Assert.That(subscriptions, Has.Some.Matches<SubscriptionTool.Subscription>(
            s => s.SubscriptionId == ExtraSubscriptionId.ToString() &&
                 s.SubscriptionName == "mcp-extra-subscription"));
    }
}
