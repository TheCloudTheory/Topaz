using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public class DataPlaneOperationResult<TResource>(
    OperationResult result,
    TResource? resource,
    string? reason,
    string? code)
{
    [JsonIgnore]
    public OperationResult Result { get; } = result;

    public TResource? Resource { get; } = resource;
    public string? Reason { get; } = reason;
    public string? Code { get; } = code;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptionsCli);
    }
}