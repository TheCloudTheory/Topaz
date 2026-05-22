using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models.Responses;

internal sealed class CheckAppServiceSiteNameResponse
{
    public bool NameAvailable { get; init; }
    public string? Message { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NoAvailabilityReason? Reason { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public enum NoAvailabilityReason
    {
        Invalid,
        AlreadyExists
    }
}
