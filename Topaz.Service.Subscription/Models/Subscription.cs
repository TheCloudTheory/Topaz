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
    public IDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();

    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Subscription()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public Subscription(SubscriptionIdentifier subscriptionIdentifier, string displayName,
        IDictionary<string, string>? tags)
    {
        SubscriptionId = subscriptionIdentifier.ToString();
        DisplayName = displayName;
        Tags = tags ?? new Dictionary<string, string>();
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptionsCli);
    }

    public void UpdateTags(string tagName, string tagValue)
    {
        if (Tags.TryAdd(tagName, tagValue)) return;
        
        Tags[tagName] = tagValue;
    }
}
