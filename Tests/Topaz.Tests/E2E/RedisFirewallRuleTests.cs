using System.Net;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class RedisFirewallRuleTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003301");

    private const string SubscriptionName = "sub-test-redis-fw";
    private const string ResourceGroupName = "rg-test-redis-fw";
    private const string CacheName = "test-redis-fw";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group", "delete",
            "--name", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, CacheName,
                new(AzureLocation.WestEurope, new RedisSku(RedisSkuName.Basic, RedisSkuFamily.BasicOrStandard, 0)));
    }

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static RedisFirewallRuleData MinimalFirewallRule() =>
        new(IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.10"));

    private async Task<RedisResource> GetCache()
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        return (await resourceGroup.Value.GetRedisAsync(CacheName)).Value;
    }

    [Test]
    public async Task RedisFirewallRule_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var cache = await GetCache();
        const string ruleName = "rule-create";

        // Act
        var result = await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, ruleName, MinimalFirewallRule());

        var rule = result.Value;

        // Assert
        Assert.That(rule, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rule.Data.Name, Is.EqualTo(ruleName));
            Assert.That(rule.Data.StartIP, Is.EqualTo(IPAddress.Parse("10.0.0.1")));
            Assert.That(rule.Data.EndIP, Is.EqualTo(IPAddress.Parse("10.0.0.10")));
        }
    }

    [Test]
    public async Task RedisFirewallRule_WhenRetrieved_IsFound()
    {
        // Arrange
        var cache = await GetCache();
        const string ruleName = "rule-get";

        await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, ruleName, MinimalFirewallRule());

        // Act
        var result = await cache.GetRedisFirewallRuleAsync(ruleName);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.Data.Name, Is.EqualTo(ruleName));
    }

    [Test]
    public async Task RedisFirewallRule_WhenUpdated_HasNewIpRange()
    {
        // Arrange
        var cache = await GetCache();
        const string ruleName = "rule-update";

        await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, ruleName, MinimalFirewallRule());

        // Act
        var updated = new RedisFirewallRuleData(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("192.168.0.255"));
        var result = await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, ruleName, updated);

        // Assert
        Assert.That(result.Value.Data.StartIP, Is.EqualTo(IPAddress.Parse("192.168.0.1")));
        Assert.That(result.Value.Data.EndIP, Is.EqualTo(IPAddress.Parse("192.168.0.255")));
    }

    [Test]
    public async Task RedisFirewallRule_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var cache = await GetCache();
        const string ruleName = "rule-delete";

        var created = await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, ruleName, MinimalFirewallRule());

        // Act
        await created.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await cache.GetRedisFirewallRuleAsync(ruleName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task RedisFirewallRule_WhenListed_ReturnsAll()
    {
        // Arrange
        var cache = await GetCache();

        await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, "rule-list-a", MinimalFirewallRule());
        await cache.GetRedisFirewallRules()
            .CreateOrUpdateAsync(WaitUntil.Completed, "rule-list-b", MinimalFirewallRule());

        // Act
        var names = new List<string>();
        await foreach (var rule in cache.GetRedisFirewallRules().GetAllAsync())
        {
            names.Add(rule.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("rule-list-a"));
        Assert.That(names, Does.Contain("rule-list-b"));
    }
}
