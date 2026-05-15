using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualNetwork.Endpoints.NetworkSecurityGroups;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

public sealed class NetworkSecurityGroupService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-nsg");

    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "network-security-group";

    public string Name => "Network Security Groups";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateNetworkSecurityGroupEndpoint(eventPipeline, logger),
        new GetNetworkSecurityGroupEndpoint(eventPipeline, logger),
        new DeleteNetworkSecurityGroupEndpoint(eventPipeline, logger),
        new ListNetworkSecurityGroupsByResourceGroupEndpoint(eventPipeline, logger),
        new ListNetworkSecurityGroupsBySubscriptionEndpoint(eventPipeline, logger),
        new UpdateNetworkSecurityGroupTagsEndpoint(eventPipeline, logger)
    ];
}
