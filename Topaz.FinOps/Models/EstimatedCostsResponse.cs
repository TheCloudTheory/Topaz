using System.Text.Json;
using Topaz.Shared;

namespace Topaz.FinOps.Models;

public class EstimatedCostsResponse
{
    public string SubscriptionId { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public double TotalMonthlyCost { get; init; }
    public IReadOnlyList<ResourceCostEntry> Resources { get; init; } = [];

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public class ResourceCostEntry
{
    public string ResourceId { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public double EstimatedMonthlyCost { get; init; }
}
