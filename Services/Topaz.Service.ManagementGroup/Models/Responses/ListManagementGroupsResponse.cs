using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models.Responses;

internal sealed class ListManagementGroupsResponse
{
    public ManagementGroup[] Value { get; init; }

    [JsonPropertyName("@nextLink")]
    public string? NextLink => null;

    public ListManagementGroupsResponse(IEnumerable<ManagementGroup> groups)
    {
        Value = groups.ToArray();
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
