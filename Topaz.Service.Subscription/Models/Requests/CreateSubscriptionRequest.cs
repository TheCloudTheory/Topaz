namespace Topaz.Service.Subscription.Models.Requests;

internal sealed class CreateSubscriptionRequest
{
    public Guid? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
}