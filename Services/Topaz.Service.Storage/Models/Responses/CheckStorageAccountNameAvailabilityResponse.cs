using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Responses;

internal sealed class CheckStorageAccountNameAvailabilityResponse
{
    public bool NameAvailable { get; init; }
    public string? Message { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NameUnavailableReason? Reason { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    internal enum NameUnavailableReason
    {
        AccountNameInvalid,
        AlreadyExists
    }
}