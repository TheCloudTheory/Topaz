using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class UpdateAcrRunRequest
{
    public bool? IsArchiveEnabled { get; init; }
}
