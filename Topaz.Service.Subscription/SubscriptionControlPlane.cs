using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

internal sealed class SubscriptionControlPlane(SubscriptionResourceProvider provider)
{
    private const string SubscriptionNotFoundMessageTemplate = "Subscription {0} not found";
    private const string SubscriptionNotFoundCode = "SubscriptionNotFound";
    
    public static SubscriptionControlPlane New(ITopazLogger logger) => new(new SubscriptionResourceProvider(logger));
    
    public ControlPlaneOperationResult<Models.Subscription> Get(SubscriptionIdentifier subscriptionIdentifier)
    {
        var data = provider.Get(subscriptionIdentifier, null, null);
        if (data == null)
            return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.NotFound, null,
                string.Format(SubscriptionNotFoundMessageTemplate, subscriptionIdentifier.Value),
                SubscriptionNotFoundCode);
        
        var model = JsonSerializer.Deserialize<Models.Subscription>(data, GlobalSettings.JsonOptions);

        return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.Success, model, null, null);
    }

    public ControlPlaneOperationResult<Models.Subscription> Create(SubscriptionIdentifier subscriptionIdentifier, string name)
    {
        var model = new Models.Subscription(subscriptionIdentifier, name);

        provider.Create(subscriptionIdentifier, null, null, model);

        return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.Created, model, null, null);
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
