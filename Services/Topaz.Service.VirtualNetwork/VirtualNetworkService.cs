using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualNetwork.Endpoints.VirtualNetworks;
using Topaz.Service.VirtualNetwork.Endpoints.Subnets;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

public sealed class VirtualNetworkService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-virtual-network");
    
    public static IReadOnlyCollection<string> Subresources => ["subnets"];
    public static string UniqueName => "virtual-network";

    public string Name => "Virtual Network";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateUpdateVirtualNetworkEndpoint(eventPipeline, logger),
        new GetVirtualNetworkEndpoint(eventPipeline, logger),
        new DeleteVirtualNetworkEndpoint(eventPipeline, logger),
        new ListVirtualNetworksByResourceGroupEndpoint(eventPipeline, logger),
        new ListVirtualNetworksBySubscriptionEndpoint(eventPipeline, logger),
        new UpdateVirtualNetworkTagsEndpoint(eventPipeline, logger),
        new CheckIpAddressAvailabilityEndpoint(eventPipeline, logger),
        new CreateOrUpdateSubnetEndpoint(eventPipeline, logger),
        new GetSubnetEndpoint(eventPipeline, logger),
        new DeleteSubnetEndpoint(eventPipeline, logger),
        new ListSubnetsEndpoint(eventPipeline, logger)
    ];
}
