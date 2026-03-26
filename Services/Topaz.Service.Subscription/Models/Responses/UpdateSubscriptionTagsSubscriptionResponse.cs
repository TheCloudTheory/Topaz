using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models.Responses;

internal sealed class UpdateSubscriptionTagsSubscriptionResponse
{
    public static UpdateSubscriptionTagsSubscriptionResponse From(string subscriptionId, string tagName,
        string tagValue)
    {
        return new UpdateSubscriptionTagsSubscriptionResponse
        {
            Id = $"/subscriptions/{subscriptionId}/tagNames/{tagName}/tagValues/{tagValue}",
            TagValue = tagValue,
            Count = new CountData { Value = 1 }
        };
    }

    public CountData? Count { get; set; }

    internal class CountData
    {
        public string Type { get; set; } = "Total";
        public int Value { get; set; }
    }

    public string? TagValue { get; set; }
    public string? Id { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptionsCli);
    }
}