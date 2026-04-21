using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models;

internal sealed class ManagementGroup
{
    public string Id { get; init; } = string.Empty;

    public string Type => "Microsoft.Management/managementGroups";

    public string Name { get; init; } = string.Empty;

    public ManagementGroupProperties Properties { get; set; } = new();

    public static ManagementGroup Create(string groupId, string displayName, ParentGroupInfo? parent)
    {
        return new ManagementGroup
        {
            Id = $"/providers/Microsoft.Management/managementGroups/{groupId}",
            Name = groupId,
            Properties = new ManagementGroupProperties
            {
                TenantId = GlobalSettings.DefaultTenantId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? groupId : displayName,
                Details = new ManagementGroupDetails
                {
                    Version = 1,
                    UpdatedTime = DateTimeOffset.UtcNow.ToString("o"),
                    Parent = parent
                }
            }
        };
    }

    public void UpdateFrom(string? displayName, ParentGroupInfo? parent)
    {
        Properties.DisplayName = string.IsNullOrWhiteSpace(displayName) ? Name : displayName;
        Properties.Details.UpdatedTime = DateTimeOffset.UtcNow.ToString("o");
        if (parent != null)
            Properties.Details.Parent = parent;
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class ManagementGroupProperties
{
    public string TenantId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ManagementGroupDetails Details { get; set; } = new();
}

internal sealed class ManagementGroupDetails
{
    public int Version { get; set; } = 1;

    public string UpdatedTime { get; set; } = DateTimeOffset.UtcNow.ToString("o");

    public ParentGroupInfo? Parent { get; set; }
}

internal sealed class ParentGroupInfo
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
