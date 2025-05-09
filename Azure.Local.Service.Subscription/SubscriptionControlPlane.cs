using System.Text.Json;
using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Subscription;

internal sealed class SubscriptionControlPlane(ResourceProvider provider)
{
    private readonly ResourceProvider provider = provider;

    public Models.Subscription Get(string subscriptionId)
    {
        var data = this.provider.Get(subscriptionId);
        var model = JsonSerializer.Deserialize<Models.Subscription>(data, GlobalSettings.JsonOptions);

        return model!;
    }

    public Models.Subscription Create(string? id, string name)
    {
        var subscriptionId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        var model = new Models.Subscription(subscriptionId, name);

        this.provider.Create(subscriptionId, model);

        return model;
    }

    internal void Delete(string subscriptionId)
    {
        this.provider.Delete(subscriptionId);
    }
}
