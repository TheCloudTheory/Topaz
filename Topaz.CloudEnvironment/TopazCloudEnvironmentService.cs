using Topaz.CloudEnvironment.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.CloudEnvironment;

public sealed class TopazCloudEnvironmentService(ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "cloudenvironment";
    public string Name => "CloudEnvironment";
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, "{resourceGroup}", ".cloud");
    public static IReadOnlyCollection<string>? Subresources => null;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new MetadataEndpoint(),
        new TenantsEndpoint(),
        new OidcEndpoint(),
        new AuthorizeEndpoint(logger),
        new TokenEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}