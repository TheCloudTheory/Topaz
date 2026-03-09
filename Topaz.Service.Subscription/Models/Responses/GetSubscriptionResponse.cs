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
            SubscriptionId = subscription.SubscriptionId,
            DisplayName = subscription.DisplayName,
            Tags = subscription.Tags
        };
    }

    public string? Id { get; set; }
    public string? SubscriptionId { get; set; }
    public string? DisplayName { get; set; }
    public IDictionary<string, string>? Tags { get; set; }


    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}