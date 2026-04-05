using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class ListUsagesResponse
{
    public RegistryUsage[] Value { get; init; } = [];

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
