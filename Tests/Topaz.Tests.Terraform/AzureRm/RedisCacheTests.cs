namespace Topaz.Tests.Terraform.AzureRm;

public class RedisCacheTests : AzureRmBatchFixture
{
    [Test]
    public void RedisCache_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("redis_basic_name"), Is.EqualTo("tf-rm-redis-basic"));
    }

    [Test]
    public void RedisCache_Hostname_ContainsCacheName()
    {
        Assert.That(GetOutput<string>("redis_basic_hostname"),
            Does.Contain("tf-rm-redis-basic"));
    }

    [Test]
    public void RedisCache_SslPort_IsDefault()
    {
        Assert.That(GetOutput<int>("redis_basic_ssl_port"), Is.EqualTo(6380));
    }

    [Test]
    public void RedisCache_MinimumTlsVersion_IsEnforced()
    {
        Assert.That(GetOutput<string>("redis_basic_tls_version"), Is.EqualTo("1.2"));
    }

    [Test]
    public void RedisCache_NonSslPort_CanBeEnabled()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("redis_no_ssl_name"), Is.EqualTo("tf-rm-redis-no-ssl"));
            Assert.That(GetOutput<bool>("redis_no_ssl_enabled"), Is.True);
            Assert.That(GetOutput<int>("redis_no_ssl_port"), Is.EqualTo(6379));
        });
    }
}
