using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public sealed class CheckNameResponse
{
    public bool NameAvailable { get; init; }
    public string? Message { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NoAvailabilityReason? Reason { get; init; } 

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public enum NoAvailabilityReason
    {
        AccountNameInvalid,
        AlreadyExists
    }
}