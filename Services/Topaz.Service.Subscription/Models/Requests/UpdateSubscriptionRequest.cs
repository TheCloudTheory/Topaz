namespace Topaz.Service.Subscription.Models.Requests;

public sealed class UpdateSubscriptionRequest
{
    public string? SubscriptionName { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
}