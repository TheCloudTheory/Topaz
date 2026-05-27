using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class ListAcrRunsResponse
{
    public AcrRunResource[] Value { get; init; } = [];
    public object? NextLink { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
