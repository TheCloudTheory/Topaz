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
