using StackExchange.Redis;

namespace Topaz.Tests.E2E.Redis;

internal sealed class RedisResp2Tests
{
    private IDatabase _db;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var muxer = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        _db = muxer.GetDatabase();
    }
    
    [Test]
    public async Task RedisResp2Tests_CanSet_AndThenGetStringValue()
    {
        await _db.StringSetAsync("key", "value");
        var str = await _db.StringGetAsync("key");
        
        Assert.AreEqual("value", str);
    }
}