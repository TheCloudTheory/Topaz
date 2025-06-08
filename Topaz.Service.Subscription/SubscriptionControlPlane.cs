using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

internal sealed class SubscriptionControlPlane(ResourceProvider provider)
{
    public Models.Subscription? Get(string subscriptionId)
    {
        var data = provider.Get(subscriptionId);
        if (data == null) return null;
        
        var model = JsonSerializer.Deserialize<Models.Subscription>(data, GlobalSettings.JsonOptions);

        return model;
    }

    public Models.Subscription Create(string? id, string name)
    {
        var subscriptionId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        var model = new Models.Subscription(subscriptionId, name);

        provider.Create(subscriptionId, model);

        return model;
    }

    internal void Delete(string subscriptionId)
    {
        provider.Delete(subscriptionId);
    }
}
