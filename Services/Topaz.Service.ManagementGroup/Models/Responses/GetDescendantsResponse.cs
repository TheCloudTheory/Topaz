using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models.Responses;

internal sealed class GetDescendantsResponse(DescendantInfo[] value)
{
    public DescendantInfo[] Value { get; } = value;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextLink { get; } = null;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
