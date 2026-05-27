using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class GetAcrRunLogSasUrlResponse
{
    public string LogLink { get; init; } = string.Empty;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
