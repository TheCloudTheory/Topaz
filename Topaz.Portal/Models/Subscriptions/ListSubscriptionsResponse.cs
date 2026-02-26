namespace Topaz.Portal.Models.Subscriptions;

public sealed class ListSubscriptionsResponse
{
    public SubscriptionDto[] Value { get; init; } = [];
}

public sealed class SubscriptionDto
{
    public string? Id { get; init; }
    public string? SubscriptionId { get; init; }
    public string? DisplayName { get; init; }
}