using System.Security.Cryptography;
using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridValidationEvent : TopazApiModel
{
    public string EventType { get; } = "Microsoft.EventGrid.SubscriptionValidationEvent";
    public required string ValidationCode { get; init; }

    public static EventGridValidationEvent New()
    {
        return new EventGridValidationEvent
        {
            ValidationCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8))
        };
    }
}