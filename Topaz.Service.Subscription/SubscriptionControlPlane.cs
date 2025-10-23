using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

internal sealed class SubscriptionControlPlane(SubscriptionResourceProvider provider)
{
    public (OperationResult result, Models.Subscription? resource) Get(SubscriptionIdentifier subscriptionIdentifier)
    {
        var data = provider.Get(subscriptionIdentifier, null, null);
        if (data == null) return (OperationResult.NotFound, null);
        
        var model = JsonSerializer.Deserialize<Models.Subscription>(data, GlobalSettings.JsonOptions);

        return (OperationResult.Success, model);
    }

    public Models.Subscription Create(string? id, string name)
    {
        var subscriptionIdentifier = string.IsNullOrEmpty(id) ? SubscriptionIdentifier.From(Guid.NewGuid().ToString()) : SubscriptionIdentifier.From(id);
        var model = new Models.Subscription(subscriptionIdentifier, name);

        provider.Create(subscriptionIdentifier, null, null, model);

        return model;
    }

    internal void Delete(SubscriptionIdentifier subscriptionIdentifier)
    {
        provider.Delete(subscriptionIdentifier, null, null);
    }

    internal (OperationResult result, Models.Subscription[] resource) List()
    {
        var rawSubscriptions = provider.List(null, null);
        var subscriptions = rawSubscriptions
            .Select(s => JsonSerializer.Deserialize<Models.Subscription>(s, GlobalSettings.JsonOptions)!).ToArray();
        
        return (OperationResult.Success, subscriptions);
    }
}
