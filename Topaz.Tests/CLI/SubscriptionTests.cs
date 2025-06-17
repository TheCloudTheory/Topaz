using Topaz.CLI;
using Topaz.Service.Subscription.Models;

namespace Topaz.Tests.CLI;

public class SubscriptionTests
{
    [Test]
    public async Task SubscriptionTests_WhenMultipleSubscriptionsExist_TheyShouldBeReturned()
    {
        var subscriptions = new List<Subscription>()
        {
            new(Guid.NewGuid().ToString(), "sub1"),
            new(Guid.NewGuid().ToString(), "sub2"),
            new(Guid.NewGuid().ToString(), "sub2")
        };

        foreach (var subscription in subscriptions)
        {
            var result = await Program.Main([
                "subscription",
                "create",
                "--id",
                subscription.SubscriptionId,
                "--name",
                subscription.DisplayName
            ]);
            
            Assert.That(result, Is.EqualTo(0));
        }
        
        var listResult = await Program.Main([
            "subscription",
            "list"
        ]);
            
        Assert.That(listResult, Is.EqualTo(0));
    }
}