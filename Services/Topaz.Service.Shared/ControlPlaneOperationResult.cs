using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public class ControlPlaneOperationResult<TResource>(
    OperationResult result,
    TResource? resource,
    string? reason = null,
    string? code = null)
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

public class ControlPlaneOperationResult(
    OperationResult result,
    string? reason = null,
    string? code = null,
    IDictionary<string, string>? headers = null)
{
    [JsonIgnore]
    public OperationResult Result { get; } = result;
    
    [JsonIgnore]
    public IDictionary<string, string>? Headers { get; } = headers;
    
    public string? Reason { get; } = reason;
    public string? Code { get; } = code;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptionsCli);
    }
}