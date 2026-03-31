using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models.Responses;

internal sealed class EnableSubscriptionResponse
{
    public string? SubscriptionId { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
