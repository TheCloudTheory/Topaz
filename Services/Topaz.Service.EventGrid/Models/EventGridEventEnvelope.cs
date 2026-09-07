using Topaz.EventPipeline.Events;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridEventEnvelope<TEventModel>
{
    public TEventModel? Event { get; init; }
    public int DeliveryAttempt { get; set; }
    public bool IsDelivered { get; set; }

    public static EventGridEventEnvelope<EventGridEventSchema> From(EventGridEventSchema message)
    {
        return new EventGridEventEnvelope<EventGridEventSchema>
        {
            Event = message
        };
    }

    public static EventGridEventEnvelope<EventGridCloudEventSchema> From(EventGridCloudEventSchema message)
    {
        return new EventGridEventEnvelope<EventGridCloudEventSchema>
        {
            Event = message
        };
    }

    public static EventGridEventEnvelope<EventGridEventSchema> From(EventGridEventPublishedEventData message)
    {
        return new EventGridEventEnvelope<EventGridEventSchema>
        {
            Event = EventGridEventSchema.From(message)
        };
    }
}