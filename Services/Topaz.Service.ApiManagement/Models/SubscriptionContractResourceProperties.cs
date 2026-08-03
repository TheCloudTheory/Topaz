using System.Text.Json.Serialization;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class SubscriptionContractResourceProperties
{
    public bool AllowTracing { get; init; }
    public DateTimeOffset? CreatedDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public DateTimeOffset? ExpirationDate { get; init; }
    public DateTimeOffset? NotificationDate { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public string? OwnerId { get; init; }
    public string? Scope { get; init; }
    public string? DisplayName { get; init; }
    public string? StateComment { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscriptionState? State { get; init; }

    internal enum SubscriptionState
    {
        Suspended,
        Active,
        Expired,
        Submitted,
        Rejected,
        Cancelled
    }
}