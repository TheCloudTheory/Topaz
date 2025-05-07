namespace Azure.Local.Service.Subscription;

internal sealed class ResourceProvider
{
    public Models.Subscription GetSubscription(string subscriptionId)
    {
        return new Models.Subscription(subscriptionId, "Azure Local");
    }
}
