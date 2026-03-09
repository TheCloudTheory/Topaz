using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models.Responses;

internal sealed class GetSubscriptionResponse
{
    public static GetSubscriptionResponse From(Subscription subscription)
    {
        return new GetSubscriptionResponse
        {
            Id = subscription.Id,
            SubscriptionName = subscription.DisplayName,
            Tags = subscription.Tags
        };
    }

    public IDictionary<string, string>? Tags { get; set; }
    public string? SubscriptionName { get; set; }
    public string? Id { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}