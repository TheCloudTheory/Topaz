using System.Text.Json;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models;

public record SubscriptionMetadata
{
    public string Id { get; init; }
    public string SubscriptionId { get; init; }
    public string DisplayName { get; init; }

    public SubscriptionMetadata(SubscriptionIdentifier subscriptionIdentifier, string displayName = "")
    {
        Id = $"/subscriptions/{subscriptionIdentifier.Value}";
        SubscriptionId = subscriptionIdentifier.Value.ToString();
        DisplayName = displayName;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
