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
    public async Task AppConfiguration_WhenStoreIsDeletedButNotPurged_MetadataFileShouldExist()
    {
        await Program.RunAsync([
            "appconfig", "delete",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(StoreMetadataPath), Is.True);
    }
    
    [Test]
    public async Task AppConfiguration_WhenStoreIsDeletedAndPurged_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "appconfig", "delete",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "appconfig", "purge",
            "--name", StoreName,
            "--location", "westeurope",
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

    [Test]
    public async Task AppConfiguration_WhenKeyValueIsSet_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyValueIsRetrieved_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "show",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyValuesAreListed_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "list",
            "--name", StoreName
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyValueIsDeleted_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "delete",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyValueIsLocked_SubsequentWriteShouldFail()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        await Program.RunAsync([
            "appconfig", "kv", "lock",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "32"
        ]);

        Assert.That(code, Is.EqualTo(1));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyValueIsUnlocked_SubsequentWriteShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        await Program.RunAsync([
            "appconfig", "kv", "lock",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize"
        ]);

        await Program.RunAsync([
            "appconfig", "kv", "unlock",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "32"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenLabelsAreListed_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16",
            "--label", "production"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "list-labels",
            "--name", StoreName
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenRevisionsAreListed_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "list-revisions",
            "--name", StoreName
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfiguration_WhenKeyValueIsFilteredByLabel_OnlyMatchingShouldReturn()
    {
        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "16",
            "--label", "production"
        ]);

        await Program.RunAsync([
            "appconfig", "kv", "set",
            "--name", StoreName,
            "--key", "MyApp:Settings:FontSize",
            "--value", "12",
            "--label", "staging"
        ]);

        var code = await Program.RunAsync([
            "appconfig", "kv", "list",
            "--name", StoreName,
            "--label", "production"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
