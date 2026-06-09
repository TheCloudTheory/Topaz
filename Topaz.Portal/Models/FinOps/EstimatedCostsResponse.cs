namespace Topaz.Portal.Models.FinOps;

public class EstimatedCostsResponse
{
    public string? SubscriptionId { get; set; }
    public string? Currency { get; set; }
    public double TotalMonthlyCost { get; set; }
    public List<ResourceCostEntry> Resources { get; set; } = [];
}

public class ResourceCostEntry
{
    public string? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public double EstimatedMonthlyCost { get; set; }
}
