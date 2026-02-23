using Topaz.Service.Authorization.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class ResourceAuthorizationService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine("{resource}", ".authorization");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "resource-authorization";
    public string Name => "Resource Authorization";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceAuthorizationEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}
