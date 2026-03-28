using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

public sealed class ContainerRegistryService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".container-registry");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "container-registry";

    public string Name => "Azure Container Registry";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ContainerRegistryEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
