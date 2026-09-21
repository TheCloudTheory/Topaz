using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
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
    private IServer _server;

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
            LoggerFactory = new RedisLoggerFactory(),
            AllowAdmin = true
        };

        var muxer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
        
        _server = muxer.GetServer(GlobalSettings.GetRedisEndpointWithPort(cacheName, false));
        _db = muxer.GetDatabase(0);
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
    
    [Test]
    public async Task RedisResp2Tests_CanSetAndAppend_AndThenGetStringValue()
    {
        await _db.StringSetAsync("key", "value");
        await _db.StringAppendAsync("key", "_appended");
        var str = await _db.StringGetAsync("key");
        
        Assert.AreEqual("value_appended", str);
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetAndDelete_AndThenGetStringValue()
    {
        await _db.StringSetAsync("key", "value");
        var str = await _db.StringGetAsync("key");

        await _db.StringDeleteAsync("key", ValueCondition.Always);
        var str2 = await _db.StringGetAsync("key");
        
        Assert.AreEqual("value", str);
        Assert.AreEqual(str2, RedisValue.Null);
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetAndDelete_AndThenCheckIfExists()
    {
        await _db.StringSetAsync("key", "value");
        var exists1 = _db.KeyExists("key");

        await _db.StringDeleteAsync("key", ValueCondition.Always);
        var exists2 = _db.KeyExists("key");
        
        Assert.AreEqual(exists1, true);
        Assert.AreEqual(exists2, false);
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetKeyAndSetExpire_AndThenCheckIfExistsAndKeyShouldNotExist()
    {
        await _db.StringSetAsync("key", "value");
        _ = _db.KeyExpire("key", TimeSpan.FromMilliseconds(1500));
        
        await Task.Delay(1500);
        var exists = _db.KeyExists("key");
        
        Assert.AreEqual(exists, false);
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetKeyAndSetExpire_AndIfUpdatedExpireIsCleared()
    {
        await _db.StringSetAsync("key", "value");
        _ = _db.KeyExpire("key", TimeSpan.FromMilliseconds(1500));
        await _db.StringSetAsync("key", "value2");
        
        await Task.Delay(1500);
        var exists = _db.KeyExists("key");
        var value = await _db.StringGetAsync("key");
        
        Assert.AreEqual(exists, true);
        Assert.AreEqual(value, "value2");
    }
    
    [Test]
    public async Task RedisResp2Tests_WhenKeyHasExpiration_TtlReturnsValue()
    {
        await _db.StringSetAsync("key", "value");
        _ = _db.KeyExpire("key", TimeSpan.FromMilliseconds(2000));
        await Task.Delay(500);
        var ttl = await _db.KeyTimeToLiveAsync("key");
        
        Assert.That(ttl, Is.LessThan(TimeSpan.FromMilliseconds(1500)));
    }
    
    [Test]
    public async Task RedisResp2Tests_WhenUsedKeys_ItReturnsAllKeys()
    {
        await _db.StringSetAsync("key", "value");
        await _db.StringSetAsync("key2", "value2");
        
        var result = (RedisResult[])(await _db.ExecuteAsync("KEYS", "*"))!;
        
        Assert.That(result, Has.Length.AtLeast(2));
    }
    
    [Test]
    public async Task RedisResp2Tests_WhenScanningKeys_ItReturnsAllKeys()
    {
        await _db.StringSetAsync("keytoscan", "value");
        await _db.StringSetAsync("keytoscan2", "value2");

        var result = _server.Keys(database: 0, pattern: "*").ToArray();
        
        Assert.That(result, Has.Length.AtLeast(2));
    }
    
    [Test]
    public async Task RedisResp2Tests_WhenScanningKeysWithSpecificGlob_ItReturnsSpecificKeysOnly()
    {
        await _db.StringSetAsync("key", "value");
        await _db.StringSetAsync("key2", "value2");
        await _db.StringSetAsync("some", "value3");
        await _db.StringSetAsync("some2", "value3");
        await _db.StringSetAsync("somekey", "value3");

        var result = _server.Keys(database: 0, pattern: "some*").ToArray();
        
        Assert.That(result, Has.Length.EqualTo(3));
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetHash_AndThenGetStringValue()
    {
        await _db.HashSetAsync("key", "foo", "bar");
        var str = await _db.HashGetAsync("key", "foo");
        
        Assert.AreEqual("bar", str);
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetHashWithMultiplePairs_AndThenGetAllValues()
    {
        await _db.HashSetAsync("key_all", [new HashEntry("foo", "bar"),  new HashEntry("foo2", "baz")]);
        var entries = await _db.HashGetAllAsync("key_all");
        
        Assert.That(entries, Has.Length.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.FirstOrDefault(e => e.Name == "foo").Name.ToString(), Is.EqualTo("foo"));
            Assert.That(entries.FirstOrDefault(e => e.Name == "foo").Value.ToString(), Is.EqualTo("bar"));
            Assert.That(entries.FirstOrDefault(e => e.Name == "foo2").Name.ToString(), Is.EqualTo("foo2"));
            Assert.That(entries.FirstOrDefault(e => e.Name == "foo2").Value.ToString(), Is.EqualTo("baz"));
        }
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetHash_AndThenDeleteValue()
    {
        await _db.HashSetAsync("key", "foo", "bar");
        var str = await _db.HashGetAsync("key", "foo");
        
        Assert.AreEqual("bar", str);
        
        await _db.HashDeleteAsync("key", "foo");
        
        Assert.AreEqual(RedisValue.Null, await _db.HashGetAsync("key", "foo"));
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSetHash_AndThenDeleteValue_WhilePreservingOthers()
    {
        await _db.HashSetAsync("key", [new HashEntry("foo", "bar"),  new HashEntry("foo2", "baz")]);
        var str = await _db.HashGetAsync("key", "foo");
        
        Assert.AreEqual("bar", str);
        
        await _db.HashDeleteAsync("key", "foo");
        
        Assert.AreEqual(RedisValue.Null, await _db.HashGetAsync("key", "foo"));
        Assert.AreEqual("baz", await _db.HashGetAsync("key", "foo2"));
    }
    
    [Test]
    public async Task RedisResp2Tests_CanPushArray_ThenRetrieveIt()
    {
        var index = await _db.ArrayInsertAsync("akey", "value1");
        
        Assert.AreEqual(index.Value, 0);
        
        index = await _db.ArrayInsertAsync("akey", "value2");
        
        Assert.AreEqual(index.Value, 1);

        var value1 = await _db.ArrayGetAsync("akey", 0);
        var value2 = await _db.ArrayGetAsync("akey", 1);
        
        Assert.AreEqual(value1, "value1");
        Assert.AreEqual(value2, "value2");
    }
    
    [Test]
    public async Task RedisResp2Tests_CanPushList_ThenRetrieveIt()
    {
        var index = await _db.ListLeftPushAsync("lkey", "value1");
        
        Assert.AreEqual(index, 1);
        
        index = await _db.ListLeftPushAsync("lkey", "value2");
        
        Assert.AreEqual(index, 2);

        var value1 = await _db.ListGetByIndexAsync("lkey", 1);
        var value2 = await _db.ListGetByIndexAsync("lkey", 2);
        
        Assert.AreEqual(value1, "value2");
        Assert.AreEqual(value2, "value1");
    }
    
    [Test]
    public async Task RedisResp2Tests_CanPushList_ThenDeleteIt()
    {
        await _db.ListLeftPushAsync("popkey", "value1");
        await _db.ListLeftPushAsync("popkey", "value2");
        await _db.ListLeftPushAsync("popkey", "value3");
        
        var value = await _db.ListLeftPopAsync("popkey");
        
        Assert.AreEqual(value, "value1");
    }
    
    [Test]
    public async Task RedisResp2Tests_CanRPushList_ThenRetrieveIt()
    {
        var length = await _db.ListRightPushAsync("rkey", "value1");
        
        Assert.AreEqual(length, 1);
        
        length = await _db.ListRightPushAsync("rkey", "value2");
        
        Assert.AreEqual(length, 2);

        var value1 = await _db.ListGetByIndexAsync("rkey", 1);
        var value2 = await _db.ListGetByIndexAsync("rkey", 2);
        
        Assert.AreEqual(value1, "value1");
        Assert.AreEqual(value2, "value2");
    }
    
    [Test]
    public async Task RedisResp2Tests_CanLPushAndRPushList_ThenRetrieveIt()
    {
        var length = await _db.ListRightPushAsync("rlkey", "value1");
        
        Assert.AreEqual(length, 1);
        
        length = await _db.ListLeftPushAsync("rlkey", "value2");
        
        Assert.AreEqual(length, 2);
        
        length = await _db.ListRightPushAsync("rlkey", "value3");
        
        Assert.AreEqual(length, 3);

        var value1 = await _db.ListGetByIndexAsync("rlkey", 1);
        var value2 = await _db.ListGetByIndexAsync("rlkey", 2);
        var value3 = await _db.ListGetByIndexAsync("rlkey", 3);
        
        Assert.AreEqual(value1, "value2");
        Assert.AreEqual(value2, "value1");
        Assert.AreEqual(value3, "value3");
    }
    
    [Test]
    public async Task RedisResp2Tests_CanRPushList_ThenDeleteIt()
    {
        await _db.ListRightPushAsync("rpopkey", "value1");
        await _db.ListRightPushAsync("rpopkey", "value2");
        await _db.ListRightPushAsync("rpopkey", "value3");
        
        var value = await _db.ListRightPopAsync("rpopkey");
        
        Assert.AreEqual(value, "value3");
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