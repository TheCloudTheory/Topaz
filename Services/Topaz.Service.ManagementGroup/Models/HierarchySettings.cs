using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models;

internal sealed class HierarchySettings
{
    public string Id { get; init; } = string.Empty;

    public string Name => "default";

    public string Type => "Microsoft.Management/managementGroups/settings";

    public HierarchySettingsProperties Properties { get; set; } = new();

    public static HierarchySettings Create(string groupId)
    {
        return new HierarchySettings
        {
            Id = $"/providers/Microsoft.Management/managementGroups/{groupId}/settings/default",
            Properties = new HierarchySettingsProperties
            {
                TenantId = GlobalSettings.DefaultTenantId
            }
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class HierarchySettingsProperties
{
    public string TenantId { get; set; } = string.Empty;

    public bool RequireAuthorizationForGroupCreation { get; set; }

    public string? DefaultManagementGroup { get; set; }
}
