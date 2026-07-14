using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class InsightsTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-0000-0000-0000-AC0100000001");
    private const string SubscriptionName = "sub-test-insights";
    private const string ResourceGroupName = "rg-test-insights";
    private const string ComponentName = "test-cli-insights";

    private string ComponentMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".insights", ComponentName, "metadata.json");

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
            "insights", "component", "create",
            "--name", ComponentName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void Insights_WhenComponentIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(ComponentMetadataPath), Is.True);
    }

    [Test]
    public async Task Insights_WhenComponentIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "insights", "component", "show",
            "--name", ComponentName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Insights_WhenComponentIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "insights", "component", "delete",
            "--name", ComponentName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(ComponentMetadataPath), Is.False);
    }

    [Test]
    public async Task Insights_WhenComponentIsUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "insights", "component", "update",
            "--name", ComponentName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--retention-in-days", "180"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Insights_WhenComponentsAreListedByResourceGroup_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "insights", "component", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Insights_WhenComponentsAreListedBySubscription_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "insights", "component", "list",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
