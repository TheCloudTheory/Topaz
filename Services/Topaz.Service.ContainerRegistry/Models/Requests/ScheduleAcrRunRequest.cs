using JetBrains.Annotations;
using Topaz.Service.Shared.Models;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class ScheduleAcrRunRequest : TopazApiRequest
{
    public string Type { get; init; } = string.Empty;

    // DockerBuildRequest fields
    public string? ContextPath { get; init; }
    public string? DockerFilePath { get; init; }
    public string[]? ImageNames { get; init; }
    public bool IsPushEnabled { get; init; }
}
