using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

public sealed class ContainerRegistryService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".container-registry");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "container-registry";

    public string Name => "Azure Container Registry";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateContainerRegistryEndpoint(eventPipeline, logger),
        new GetContainerRegistryEndpoint(eventPipeline, logger),
        new ListContainerRegistriesByResourceGroupEndpoint(eventPipeline, logger),
        new ListContainerRegistriesBySubscriptionEndpoint(eventPipeline, logger),
        new DeleteContainerRegistryEndpoint(eventPipeline, logger),
        new UpdateContainerRegistryEndpoint(eventPipeline, logger),
        new CheckContainerRegistryNameAvailabilityEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
