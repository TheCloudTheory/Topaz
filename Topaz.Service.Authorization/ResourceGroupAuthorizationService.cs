using Topaz.Service.Authorization.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class ResourceGroupAuthorizationService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".authorization");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "resource-group-authorization";
    public string Name => "Resource Group Authorization";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceGroupAuthorizationEndpoint(logger)
    ];
}
