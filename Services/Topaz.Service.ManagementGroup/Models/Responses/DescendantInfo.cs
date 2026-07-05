using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models.Responses;

internal sealed class DescendantInfo
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public DescendantInfoProperties Properties { get; init; } = new();

    public static DescendantInfo FromManagementGroup(ManagementGroup mg, string parentArmId) =>
        new()
        {
            Id = mg.Id,
            Type = "Microsoft.Management/managementGroups",
            Name = mg.Name,
            Properties = new DescendantInfoProperties
            {
                DisplayName = mg.Properties.DisplayName,
                Parent = new DescendantParentInfo { Id = parentArmId }
            }
        };

    public static DescendantInfo FromSubscription(ManagementGroupSubscription sub, string parentArmId) =>
        new()
        {
            Id = $"/subscriptions/{sub.Name}",
            Type = "/subscriptions",
            Name = sub.Name,
            Properties = new DescendantInfoProperties
            {
                DisplayName = sub.Properties.DisplayName,
                Parent = new DescendantParentInfo { Id = parentArmId }
            }
        };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class DescendantInfoProperties
{
    public string DisplayName { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DescendantParentInfo? Parent { get; init; }
}

internal sealed class DescendantParentInfo
{
    public string Id { get; init; } = string.Empty;
}
