using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupService(ITopazLogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".resource-groups";
    public static IReadOnlyCollection<string>? Subresources => null;

    public string Name => "Resource Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceGroupEndpoint(new ResourceGroupResourceProvider(logger), logger)
    ];
}
