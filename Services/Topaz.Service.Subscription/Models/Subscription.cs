using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models;

internal sealed class Subscription
{
    public string Id
    {
        get;
        init;
    }
    
    public string SubscriptionId { get; init; }
    public string? DisplayName { get; set; }
    public string State { get; set; } = "Enabled";
    public IDictionary<string, string> Tags { get; set; }

    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Subscription()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public Subscription(SubscriptionIdentifier subscriptionIdentifier, string displayName,
        IDictionary<string, string>? tags)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}";
        SubscriptionId = subscriptionIdentifier.ToString();
        DisplayName = displayName;
        Tags = tags ?? new Dictionary<string, string>();
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public void UpdateTags(string tagName, string tagValue)
    {
        if (Tags.TryAdd(tagName, tagValue)) return;
        
        Tags[tagName] = tagValue;
    }

    public void UpdateFrom(UpdateSubscriptionRequest request)
    {
        DisplayName = request.SubscriptionName;
        Tags = request.Tags ?? new Dictionary<string, string>();
    }
}
