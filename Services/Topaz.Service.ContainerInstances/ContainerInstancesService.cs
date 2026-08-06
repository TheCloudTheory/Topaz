using Topaz.EventPipeline;
using Topaz.Service.ContainerInstances.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerInstances;

public sealed class ContainerInstancesService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".aci");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "aci";

    public string Name => "Container Instances";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateContainerGroupEndpoint(eventPipeline, logger)
    ];
}