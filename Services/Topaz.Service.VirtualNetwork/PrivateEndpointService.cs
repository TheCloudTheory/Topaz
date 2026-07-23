using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualNetwork.Endpoints.PrivateEndpoints;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

public sealed class PrivateEndpointService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".private-endpoint");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "private-endpoint";
    public string Name => "Private Endpoint";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdatePrivateEndpointEndpoint(eventPipeline, logger),
        new GetPrivateEndpointEndpoint(eventPipeline, logger),
        new DeletePrivateEndpointEndpoint(eventPipeline, logger),
        new ListPrivateEndpointsByResourceGroupEndpoint(eventPipeline, logger),
        new ListPrivateEndpointsBySubscriptionEndpoint(eventPipeline, logger)
    ];
}