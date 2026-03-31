using System.Text.Json;
using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

internal sealed class SubscriptionControlPlane(Pipeline eventPipeline, SubscriptionResourceProvider provider, ITopazLogger logger)
{
    private const string SubscriptionNotFoundMessageTemplate = "Subscription {0} not found";
    private const string SubscriptionNotFoundCode = "SubscriptionNotFound";

    public static SubscriptionControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new SubscriptionResourceProvider(logger), logger);
    
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

    public ControlPlaneOperationResult<Models.Subscription> Create(SubscriptionIdentifier subscriptionIdentifier,
        string name, IDictionary<string, string>? tags)
    {
        var model = new Models.Subscription(subscriptionIdentifier, name, tags);

        provider.Create(subscriptionIdentifier, null, null, model);
        
        // We publish this particular event because there are other services (like Authorization)
        // which will listen to it and perform additional actions (like assigning super admin
        // to a new subscription).
        eventPipeline.TriggerEvent<SubscriptionCreatedEventData, IEventDefinition<SubscriptionCreatedEventData>>(new SubscriptionCreatedEvent
        {
            Data = new SubscriptionCreatedEventData
            {
                SubscriptionId = subscriptionIdentifier.Value.ToString()
            }
        });

        return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.Created, model, null, null);
    }

    internal void Delete(SubscriptionIdentifier subscriptionIdentifier)
    {
        provider.Delete(subscriptionIdentifier, null, null);
    }

    internal ControlPlaneOperationResult<Models.Subscription[]> List()
    {
        var rawSubscriptions = provider.List(null, null);
        var subscriptions = rawSubscriptions
            .Select(s => JsonSerializer.Deserialize<Models.Subscription>(s, GlobalSettings.JsonOptions)!).ToArray();
        
        return new ControlPlaneOperationResult<Models.Subscription[]>(OperationResult.Success, subscriptions, null, null);
    }

    public ControlPlaneOperationResult UpdateTags(SubscriptionIdentifier subscriptionIdentifier, string tagName, string tagValue)
    {
        var subscription = Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, SubscriptionNotFoundMessageTemplate,
                SubscriptionNotFoundCode);
        }
        
        subscription.Resource.UpdateTags(tagName, tagValue);
        
        Update(subscriptionIdentifier, new UpdateSubscriptionRequest
        {
            SubscriptionName = subscription.Resource.DisplayName,
            Tags = subscription.Resource.Tags
        });
        
        return new ControlPlaneOperationResult(OperationResult.Updated, null, null);
    }

    public ControlPlaneOperationResult<Models.Subscription> Update(SubscriptionIdentifier subscriptionIdentifier,
        UpdateSubscriptionRequest request)
    {
        var subscriptionOperation = Get(subscriptionIdentifier);
        if (subscriptionOperation.Resource == null || subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.NotFound, null,
                SubscriptionNotFoundMessageTemplate, SubscriptionNotFoundCode);
        }
        
        subscriptionOperation.Resource.UpdateFrom(request);
        
        provider.CreateOrUpdate(subscriptionIdentifier, null, null, subscriptionOperation.Resource);
        return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.Updated, subscriptionOperation.Resource,
            null, null);
    }

    public ControlPlaneOperationResult<Models.Subscription[]> ListLocations(SubscriptionIdentifier subscriptionIdentifier)
    {
        logger.LogDebug(nameof(SubscriptionControlPlane), nameof(ListLocations),
            "Executing {0}: {1}", nameof(ListLocations), subscriptionIdentifier.Value);

        var subscriptionOperation = Get(subscriptionIdentifier);
        if (subscriptionOperation.Resource == null || subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<Models.Subscription[]>(OperationResult.NotFound, null,
                string.Format(SubscriptionNotFoundMessageTemplate, subscriptionIdentifier.Value),
                SubscriptionNotFoundCode);
        }

        return new ControlPlaneOperationResult<Models.Subscription[]>(OperationResult.Success, null, null, null);
    }

    public ControlPlaneOperationResult<Models.Subscription> Cancel(SubscriptionIdentifier subscriptionIdentifier)
    {
        logger.LogDebug(nameof(SubscriptionControlPlane), nameof(Cancel),
            "Executing {0}: {1}", nameof(Cancel), subscriptionIdentifier.Value);

        var subscriptionOperation = Get(subscriptionIdentifier);
        if (subscriptionOperation.Resource == null || subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.NotFound, null,
                string.Format(SubscriptionNotFoundMessageTemplate, subscriptionIdentifier.Value),
                SubscriptionNotFoundCode);
        }

        subscriptionOperation.Resource.State = "Disabled";

        provider.CreateOrUpdate(subscriptionIdentifier, null, null, subscriptionOperation.Resource);
        return new ControlPlaneOperationResult<Models.Subscription>(OperationResult.Updated, subscriptionOperation.Resource,
            null, null);
    }
}
