using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

internal sealed class SubscriptionControlPlane(SubscriptionResourceProvider provider)
{
    public (OperationResult result, Models.Subscription? resource) Get(SubscriptionIdentifier subscriptionIdentifier)
    {
        var data = provider.Get(subscriptionIdentifier.ToString());
        if (data == null) return (OperationResult.NotFound, null);
        
        var model = JsonSerializer.Deserialize<Models.Subscription>(data, GlobalSettings.JsonOptions);

        return (OperationResult.Success, model);
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

    internal (OperationResult result, Models.Subscription[] resource) List()
    {
        var rawSubscriptions = provider.List();
        var subscriptions = rawSubscriptions
            .Select(s => JsonSerializer.Deserialize<Models.Subscription>(s, GlobalSettings.JsonOptions)!).ToArray();
        
        return (OperationResult.Success, subscriptions);
    }
}
