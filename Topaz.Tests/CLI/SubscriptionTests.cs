using Topaz.CLI;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models;

namespace Topaz.Tests.CLI;

public class SubscriptionTests
{
    [Test]
    public async Task SubscriptionTests_WhenMultipleSubscriptionsExist_TheyShouldBeReturned()
    {
        var subscriptions = new List<Subscription>
        {
            new(SubscriptionIdentifier.From(Guid.NewGuid().ToString()), "sub1", null),
            new(SubscriptionIdentifier.From(Guid.NewGuid().ToString()), "sub2", null),
            new(SubscriptionIdentifier.From(Guid.NewGuid().ToString()), "sub2", null)
        };

        foreach (var subscription in subscriptions)
        {
            var result = await Program.RunAsync([
                "subscription",
                "create",
                "--id",
                subscription.SubscriptionId,
                "--name",
                subscription.DisplayName
            ]);
            
            Assert.That(result, Is.EqualTo(0));
        }
        
        var listResult = await Program.RunAsync([
            "subscription",
            "list"
        ]);
            
        Assert.That(listResult, Is.EqualTo(0));
    }
}