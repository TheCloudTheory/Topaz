using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridDataPlane(EventGridTopicControlPlane controlPlane)
{
    public static EventGridDataPlane New(EventGridTopicControlPlane controlPlane) => new(controlPlane);

    public void PublishEventGridEvent()
    {
        _ = controlPlane.Get(SubscriptionIdentifier.Empty, ResourceGroupIdentifier.From(""), "");
    }
}