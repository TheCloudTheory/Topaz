using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E.Redis;

internal sealed class RedisResp2Tests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("7944239E-0F3E-4397-ABDD-49EB54D4560F");

    private const string SubscriptionName = "sub-test-redis-data";
    private const string ResourceGroupName = "rg-test-redis-data";
    
    private IDatabase _db;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
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
        const string cacheName = "test-redis-data";
        
        _ = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, MinimalRedisContent());
        
        var cache = await resourceGroup.Value.GetRedisAsync(cacheName);
        var keys = await cache.Value.GetKeysAsync();

        var configurationOptions = new ConfigurationOptions
        {
            EndPoints = { GlobalSettings.GetRedisEndpointWithPort(cacheName, false) },
            Password = keys.Value.PrimaryKey,
            LoggerFactory = new RedisLoggerFactory()
        };
        
        var muxer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
        _db = muxer.GetDatabase();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);
    }
    
    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
    
    private static RedisCreateOrUpdateContent MinimalRedisContent() =>
        new(AzureLocation.WestEurope, new RedisSku(RedisSkuName.Basic, RedisSkuFamily.BasicOrStandard, 0));
    
    [Test]
    public async Task RedisResp2Tests_CanSet_AndThenGetStringValue()
    {
        await _db.StringSetAsync("key", "value");
        var str = await _db.StringGetAsync("key");
        
        Assert.AreEqual("value", str);
    }
    
    [UsedImplicitly]
    public class RedisLoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new PrettyTopazLogger("redis");
            logger.EnableLoggingToFile(true);
            
            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}