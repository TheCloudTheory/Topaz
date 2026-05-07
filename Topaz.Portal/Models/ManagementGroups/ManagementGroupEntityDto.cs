namespace Topaz.Portal.Models.ManagementGroups;

public sealed class ManagementGroupEntityDto
{
    public string Id { get; init; } = string.Empty;

    /// <summary>"Microsoft.Management/managementGroups" or "/subscriptions"</summary>
    public string Type { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>ARM ID of the parent management group, or null for root groups.</summary>
    public string? ParentId { get; init; }

    public bool IsManagementGroup =>
        string.Equals(Type, "Microsoft.Management/managementGroups", StringComparison.OrdinalIgnoreCase);

    public bool IsSubscription =>
        string.Equals(Type, "/subscriptions", StringComparison.OrdinalIgnoreCase);
}
