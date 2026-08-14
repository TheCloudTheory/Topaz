using Topaz.Portal.Components.Pages.Redis;
using Topaz.Portal.Models.Redis;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class RedisCachesPageEmptyListTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void RedisCachesPage_EmptyList_ShowsNoCachesMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListRedisCaches()
            .Returns(Task.FromResult(new ListRedisCachesResponse { Value = [] }));

        var cut = Render<RedisCaches>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Redis caches found")));
    }
}

[TestFixture]
public class RedisCachesPageWithCachesTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void RedisCachesPage_WithCaches_ShowsTable()
    {
        var subId = Guid.NewGuid();
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));
        _client.ListRedisCaches()
            .Returns(Task.FromResult(new ListRedisCachesResponse
            {
                Value =
                [
                    new RedisDto
                    {
                        Id = $"/subscriptions/{subId}/resourceGroups/rg1/providers/Microsoft.Cache/Redis/myredis",
                        Name = "myredis",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        SkuName = "Basic",
                        RedisVersion = "6",
                        HostName = "myredis.redis.cache.windows.net"
                    }
                ]
            }));

        var cut = Render<RedisCaches>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("myredis")), Is.True,
                "Expected the cache name to appear in the table.");
        });
    }
}

[TestFixture]
public class RedisCachesPageCreateTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task RedisCachesPage_CreatePanel_CreatesAndRefreshesList()
    {
        var subId = Guid.NewGuid();
        const string cacheName = "mynewredis";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListRedisCaches()
            .Returns(
                Task.FromResult(new ListRedisCachesResponse { Value = [] }),
                Task.FromResult(new ListRedisCachesResponse
                {
                    Value =
                    [
                        new RedisDto
                        {
                            Name = cacheName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope",
                            SkuName = "Basic"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateRedisCache(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<RedisCaches>();

        // Wait for initial load
        await cut.WaitForAssertionAsync(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Redis caches found")));

        // Open create panel
        await cut.Find("button.btn-primary").ClickAsync();

        // Select subscription
        await cut.Find("select").ChangeAsync(subId.ToString("D"));

        // Wait for resource group dropdown to populate
        await cut.WaitForAssertionAsync(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        // Select resource group
        var selects = cut.FindAll("select");
        await selects[1].ChangeAsync("rg1");

        // Fill in cache name
        await cut.Find("input[placeholder='e.g. my-redis-cache']").ChangeAsync(cacheName);

        // Submit
        await cut.Find("button.btn-success").ClickAsync();

        // Assert new cache appears in the list
        await cut.WaitForAssertionAsync(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(cacheName)), Is.True,
                "Expected the new cache name to appear in the table.");
        });

        await _client.Received(1).CreateRedisCache(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == cacheName),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }
}
