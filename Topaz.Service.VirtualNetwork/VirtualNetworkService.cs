using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

public sealed class VirtualNetworkService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-virtual-network");
    
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "virtual-network";

    public string Name => "Virtual Network";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new VirtualNetworkServiceEndpoint(logger)
    ];
}
