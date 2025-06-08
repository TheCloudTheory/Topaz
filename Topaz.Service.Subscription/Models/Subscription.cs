using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models;

public record class Subscription
{
    public string Id => $"/subscriptions/{SubscriptionId}";
    public string SubscriptionId { get; set; }
    public string DisplayName { get; set; }

    public Subscription(string subscriptionId, string displayName)
    {
        SubscriptionId = subscriptionId;
        DisplayName = displayName;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
