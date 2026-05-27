using System.Text.Json;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class ListAcrTasksResponse
{
    public AcrTaskResource[] Value { get; init; } = [];

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
