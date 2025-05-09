using System.Text.Json;
using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Subscription.Models;

public record class Subscription(string SubscriptionId, string DisplayName)
{
    public string Id => $"/subscriptions/{SubscriptionId}";

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
