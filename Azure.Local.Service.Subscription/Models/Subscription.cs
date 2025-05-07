namespace Azure.Local.Service.Subscription.Models;

public record class Subscription(string SubscriptionId, string DisplayName)
{
    public string Id => $"/subscriptions/{SubscriptionId}";
}
