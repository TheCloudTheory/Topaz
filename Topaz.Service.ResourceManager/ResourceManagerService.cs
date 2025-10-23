using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerService(ITopazLogger logger) : IServiceDefinition
{
    public string Name => "Resource Manager";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".resource-manager");
    public static IReadOnlyCollection<string>? Subresources => [];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ResourceManagerEndpoint(logger),
    ];
}