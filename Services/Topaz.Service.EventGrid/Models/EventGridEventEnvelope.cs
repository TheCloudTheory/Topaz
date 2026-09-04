namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridEventEnvelope
{
    public EventGridEventSchema? Event { get; init; }
    public int DeliveryAttempt { get; set; }
    public bool IsDelivered { get; set; }

    public static EventGridEventEnvelope From(EventGridEventSchema message)
    {
        return new EventGridEventEnvelope
        {
            Event = message
        };
    }
}