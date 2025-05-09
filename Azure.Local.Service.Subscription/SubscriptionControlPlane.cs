using System.Text.Json;

namespace Azure.Local.Service.Subscription;

internal sealed class SubscriptionControlPlane(ResourceProvider provider)
{
    private readonly ResourceProvider provider = provider;

    public Models.Subscription Get(string subscriptionId)
    {
        var data = this.provider.Get(subscriptionId);
        var model = JsonSerializer.Deserialize<Models.Subscription>(data);

        return model!;
    }

    public Models.Subscription Create(string name)
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var model = new Models.Subscription(subscriptionId, name);

        this.provider.Create(subscriptionId, model);

        return model;
    }

    internal void Delete(string subscriptionId)
    {
        this.provider.Delete(subscriptionId);
    }
}
