using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class AppConfigurationTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-0000-0000-0000-AC0100000000");
    private const string SubscriptionName = "sub-test-appconfig";
    private const string ResourceGroupName = "rg-test-appconfig";
    private const string StoreName = "test-cli-appconfig";

    private string StoreMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".app-configuration", StoreName, "metadata.json");

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
            "appconfig", "create",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void AppConfiguration_WhenStoreIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(StoreMetadataPath), Is.True);
    }

    [Test]
    public async Task AppConfiguration_WhenStoreIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "show",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenStoreIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "appconfig", "delete",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(StoreMetadataPath), Is.False);
    }

    [Test]
    public async Task AppConfiguration_WhenStoreIsUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "update",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--tags", "env=test"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenStoresAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeysAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "list-keys",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyIsRegenerated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "regenerate-key",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--key-id", "Primary"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
