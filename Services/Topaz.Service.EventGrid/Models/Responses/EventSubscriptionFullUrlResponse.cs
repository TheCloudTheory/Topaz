using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models.Responses;

internal sealed class EventSubscriptionFullUrlResponse : TopazApiModel
{
    public string? EndpointUrl { get; set; }

    public static EventSubscriptionFullUrlResponse From(string endpoint)
    {
        return new EventSubscriptionFullUrlResponse { EndpointUrl = endpoint };
    }
}