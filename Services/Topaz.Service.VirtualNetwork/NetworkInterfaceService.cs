using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualNetwork.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

public sealed class NetworkInterfaceService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-nic");

    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "network-interface";

    public string Name => "Network Interfaces";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateNetworkInterfaceEndpoint(eventPipeline, logger),
        new GetNetworkInterfaceEndpoint(eventPipeline, logger),
        new DeleteNetworkInterfaceEndpoint(eventPipeline, logger),
        new ListNetworkInterfacesByResourceGroupEndpoint(eventPipeline, logger),
        new ListNetworkInterfacesBySubscriptionEndpoint(eventPipeline, logger),
        new UpdateNetworkInterfaceTagsEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
