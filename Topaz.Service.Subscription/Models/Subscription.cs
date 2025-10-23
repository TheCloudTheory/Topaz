using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models;

public record Subscription
{
    public string Id => $"/subscriptions/{SubscriptionId}";
    public string SubscriptionId { get; init; }
    public string DisplayName { get; init; }

    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Subscription()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public Subscription(SubscriptionIdentifier subscriptionIdentifier, string displayName)
    {
        SubscriptionId = subscriptionIdentifier.ToString();
        DisplayName = displayName;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptionsCli);
    }
}
