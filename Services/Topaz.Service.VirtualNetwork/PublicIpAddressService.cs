using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualNetwork.Endpoints.PublicIpAddresses;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

public sealed class PublicIpAddressService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-pip");

    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "public-ip-address";

    public string Name => "Public IP Addresses";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdatePublicIpAddressEndpoint(eventPipeline, logger),
        new GetPublicIpAddressEndpoint(eventPipeline, logger),
        new DeletePublicIpAddressEndpoint(eventPipeline, logger),
        new ListPublicIpAddressesByResourceGroupEndpoint(eventPipeline, logger),
        new ListPublicIpAddressesBySubscriptionEndpoint(eventPipeline, logger),
        new UpdatePublicIpAddressTagsEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
