using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class RedisTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-0000-0000-0000-AC0200000000");
    private const string SubscriptionName = "sub-test-redis";
    private const string ResourceGroupName = "rg-test-redis";
    private const string CacheName = "test-cli-redis";

    private static string CacheMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".redis", CacheName, "metadata.json");

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
            "redis", "create",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--sku", "Standard"
        ]);
    }

    [Test]
    public void Redis_WhenCacheIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(CacheMetadataPath), Is.True);
    }

    [Test]
    public async Task Redis_WhenCacheIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "show",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenCacheIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "redis", "delete",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(CacheMetadataPath), Is.False);
    }

    [Test]
    public async Task Redis_WhenCacheIsUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "update",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--tags", "env=test"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenCachesAreListedByResourceGroup_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenCachesAreListedBySubscription_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "list",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenKeysAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "list-keys",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenPrimaryKeyIsRegenerated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "regenerate-key",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--key-type", "Primary"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenSecondaryKeyIsRegenerated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "redis", "regenerate-key",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--key-type", "Secondary"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenFirewallRuleIsCreated_MetadataFileShouldExist()
    {
        await Program.RunAsync([
            "redis", "firewall-rule", "create",
            "--name", CacheName,
            "--rule-name", "allow-test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--start-ip", "10.0.0.1",
            "--end-ip", "10.0.0.100"
        ]);

        var path = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
            ".resource-group", ResourceGroupName, ".redis", CacheName, "firewall-rules", "allow-test", "metadata.json");

        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public async Task Redis_WhenFirewallRuleIsRetrieved_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "redis", "firewall-rule", "create",
            "--name", CacheName,
            "--rule-name", "allow-test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--start-ip", "10.0.0.1",
            "--end-ip", "10.0.0.100"
        ]);

        var code = await Program.RunAsync([
            "redis", "firewall-rule", "show",
            "--name", CacheName,
            "--rule-name", "allow-test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenFirewallRulesAreListed_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "redis", "firewall-rule", "create",
            "--name", CacheName,
            "--rule-name", "allow-test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--start-ip", "10.0.0.1",
            "--end-ip", "10.0.0.100"
        ]);

        var code = await Program.RunAsync([
            "redis", "firewall-rule", "list",
            "--name", CacheName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task Redis_WhenFirewallRuleIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "redis", "firewall-rule", "create",
            "--name", CacheName,
            "--rule-name", "allow-test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--start-ip", "10.0.0.1",
            "--end-ip", "10.0.0.100"
        ]);

        await Program.RunAsync([
            "redis", "firewall-rule", "delete",
            "--name", CacheName,
            "--rule-name", "allow-test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var path = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
            ".resource-group", ResourceGroupName, ".redis", CacheName, "firewall-rules", "allow-test", "metadata.json");

        Assert.That(File.Exists(path), Is.False);
    }
}
