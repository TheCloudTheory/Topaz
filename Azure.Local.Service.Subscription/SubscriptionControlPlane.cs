using System.Text.Json;

namespace Azure.Local.Service.Subscription;

internal sealed class SubscriptionControlPlane(ResourceProvider provider)
{
    private readonly ResourceProvider provider = provider;

    public Models.Subscription? Get(string subscriptionId)
    {
        var data = this.provider.Get(subscriptionId);
        var model = JsonSerializer.Deserialize<Models.Subscription>(data);

        return model;
    }
}
