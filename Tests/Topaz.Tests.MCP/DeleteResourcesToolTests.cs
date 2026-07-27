using Azure.ResourceManager;
using NUnit.Framework;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.ResourceManager;

namespace Topaz.Tests.MCP;

[TestFixture]
public class DeleteResourcesToolTests
{
    private const string DeleteTargetResourceGroup = "rg-mcp-delete-test";

    [SetUp]
    public async Task EnsureResourceGroupExists()
    {
        await Program.RunAsync([
            "group", "create",
            "--name", DeleteTargetResourceGroup,
            "--location", "eastus",
            "--subscription-id", McpTestFixture.SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task DeleteResourceGroup_ReturnsDeletedTrue()
    {
        var result = await DeleteResourcesTool.DeleteResourceGroup(
            McpTestFixture.SubscriptionId,
            DeleteTargetResourceGroup,
            McpTestFixture.ObjectId);

        Assert.That(result.Deleted, Is.True);
    }

    [Test]
    public async Task DeleteResourceGroup_ResourceGroupNoLongerExists()
    {
        await DeleteResourcesTool.DeleteResourceGroup(
            McpTestFixture.SubscriptionId,
            DeleteTargetResourceGroup,
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var exists = await subscription.GetResourceGroups().ExistsAsync(DeleteTargetResourceGroup);

        Assert.That(exists.Value, Is.False);
    }
}
