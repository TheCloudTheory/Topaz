using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerService(ITopazLogger logger) : IServiceDefinition
{
    public string Name => "Resource Manager";
    public static string LocalDirectoryPath => ".resource-manager";
    public static IReadOnlyCollection<string>? Subresources => [];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ResourceManagerEndpoint(logger),
    ];
}