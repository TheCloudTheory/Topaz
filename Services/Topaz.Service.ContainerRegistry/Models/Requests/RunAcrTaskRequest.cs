using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class RunAcrTaskRequest
{
    public RunAcrTaskRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    internal sealed class RunAcrTaskRequestProperties
    {
        public JsonElement? OverrideTaskStepProperties { get; init; }
        public bool? IsArchiveEnabled { get; init; }
    }
}
