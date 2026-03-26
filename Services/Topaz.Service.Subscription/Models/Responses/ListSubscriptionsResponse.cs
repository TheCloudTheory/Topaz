using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models.Responses;

internal class ListSubscriptionsResponse
{
    [Obsolete]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ListSubscriptionsResponse()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ListSubscriptionsResponse(Subscription[] subscriptions)
    {
        Value = subscriptions;
    }
    
    public Subscription[] Value { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}