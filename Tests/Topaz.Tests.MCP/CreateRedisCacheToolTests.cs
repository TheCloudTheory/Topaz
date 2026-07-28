using NUnit.Framework;
using Topaz.CLI;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateRedisCacheToolTests
{
    private const string CacheName = "mcp-redis-cache";

    [OneTimeSetUp]
    public async Task DeleteLeftoverCache()
    {
        await Program.RunAsync([
            "redis", "delete",
            "--name", CacheName,
            "-g", McpTestFixture.ResourceGroupName,
            "--subscription-id", McpTestFixture.SubscriptionId.ToString()
        ]);
    }

    [Test, Order(1)]
    public async Task CreateRedisCache_ReturnsCacheName()
    {
        var result = await CreateRedisCacheTool.CreateRedisCache(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            CacheName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.CacheName, Is.EqualTo(CacheName));
    }

    [Test, Order(1)]
    public async Task CreateRedisCache_ReturnsHostName()
    {
        var result = await CreateRedisCacheTool.CreateRedisCache(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            CacheName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.HostName, Is.EqualTo($"{CacheName}.redis.cache.topaz.local.dev"));
    }

    [Test, Order(1)]
    public async Task CreateRedisCache_ReturnsSslPort()
    {
        var result = await CreateRedisCacheTool.CreateRedisCache(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            CacheName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.SslPort, Is.EqualTo(6380));
    }

    [Test, Order(1)]
    public async Task CreateRedisCache_ReturnsPrimaryKey()
    {
        var result = await CreateRedisCacheTool.CreateRedisCache(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            CacheName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.PrimaryKey, Is.Not.Null.And.Not.Empty);
    }

    [Test, Order(1)]
    public async Task CreateRedisCache_ReturnsConnectionString()
    {
        var result = await CreateRedisCacheTool.CreateRedisCache(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            CacheName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.Multiple(() =>
        {
            Assert.That(result.ConnectionString, Does.Contain($"{CacheName}.redis.cache.topaz.local.dev:6380"));
            Assert.That(result.ConnectionString, Does.Contain("password="));
            Assert.That(result.ConnectionString, Does.Contain("ssl=True"));
            Assert.That(result.ConnectionString, Does.Contain("abortConnect=False"));
        });
    }
}
