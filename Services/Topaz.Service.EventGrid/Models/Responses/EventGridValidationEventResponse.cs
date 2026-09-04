using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models.Responses;

internal sealed class EventGridValidationEventResponse : TopazApiModel
{
    public string? ValidationResponse { get; set; }
}