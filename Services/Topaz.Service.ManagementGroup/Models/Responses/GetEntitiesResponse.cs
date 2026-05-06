using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models.Responses;

internal sealed class GetEntitiesResponse
{
    public EntityInfo[] Value { get; init; }

    [JsonPropertyName("@nextLink")]
    public string? NextLink => null;

    public GetEntitiesResponse(IEnumerable<EntityInfo> entities)
    {
        Value = entities.ToArray();
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
