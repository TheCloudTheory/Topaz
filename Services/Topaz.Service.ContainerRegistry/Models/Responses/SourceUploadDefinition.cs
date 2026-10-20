using JetBrains.Annotations;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

[UsedImplicitly]
internal sealed class SourceUploadDefinition : TopazApiModel
{
    public string? UploadUrl { get; set; }
    public string? RelativePath { get; set; }

    public static SourceUploadDefinition? FromRegistry(ContainerRegistryResource registry)
    {
        var relativePath = $"source/{DateTime.UtcNow:yyyyMMddHHmm}/{Guid.NewGuid()}.tar.gz";
        return new SourceUploadDefinition
        {
            RelativePath = relativePath,
            UploadUrl = $"{GlobalSettings.GetBlobStorageEndpoint(registry.Name)}/container/{relativePath}"
        };
    }
}