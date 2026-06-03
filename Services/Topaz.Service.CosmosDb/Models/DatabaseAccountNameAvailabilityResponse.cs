using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class DatabaseAccountNameAvailabilityResponse
{
    public bool NameAvailable { get; init; }
    public string? Message { get; init; }
    public string? Reason { get; init; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
