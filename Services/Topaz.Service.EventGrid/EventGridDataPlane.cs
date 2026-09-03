using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridDataPlane(EventGridTopicControlPlane controlPlane)
{
    public static EventGridDataPlane New(EventGridTopicControlPlane controlPlane) => new(controlPlane);

    public DataPlaneOperationResult PublishEventGridEvent(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string topicName)
    {
        var topicOperation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicOperation.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, topicOperation.Reason, topicOperation.Code);
        }
        
        return new DataPlaneOperationResult(OperationResult.Success);
    }
}