using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models.Responses;

internal sealed class EntityInfo
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public EntityInfoProperties Properties { get; init; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class EntityInfoProperties
{
    public string TenantId { get; init; } = GlobalSettings.DefaultTenantId;

    public string DisplayName { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EntityParentInfo? Parent { get; init; }

    public string Permissions { get; init; } = "edit";

    public string InheritedPermissions { get; init; } = "edit";

    public int NumberOfDescendants { get; init; }

    public int NumberOfChildren { get; init; }

    public int NumberOfChildGroups { get; init; }

    public string[] ParentDisplayNameChain { get; init; } = [];

    public string[] ParentNameChain { get; init; } = [];
}

internal sealed class EntityParentInfo
{
    public string Id { get; init; } = string.Empty;
}
