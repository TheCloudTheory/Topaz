using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class ScheduleAcrRunRequest
{
    public string Type { get; init; } = string.Empty;
}
