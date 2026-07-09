using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class LogAnalyticsTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-0000-0000-0000-AC0200000000");
    private const string SubscriptionName = "sub-test-loganalytics";
    private const string ResourceGroupName = "rg-test-loganalytics";
    private const string WorkspaceName = "test-cli-workspace";

    private string WorkspaceMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".log-analytics", WorkspaceName, "metadata.json");

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync([
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "loganalytics", "create",
            "--name", WorkspaceName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void LogAnalytics_WhenWorkspaceIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(WorkspaceMetadataPath), Is.True);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "loganalytics", "show",
            "--name", WorkspaceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "loganalytics", "delete",
            "--name", WorkspaceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(WorkspaceMetadataPath), Is.False);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsUpdatedWithTags_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "loganalytics", "update",
            "--name", WorkspaceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--tags", "env=test"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsUpdatedWithRetention_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "loganalytics", "update",
            "--name", WorkspaceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--retention-in-days", "60"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspacesAreListedByResourceGroup_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "loganalytics", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspacesAreListedBySubscription_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "loganalytics", "list",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
