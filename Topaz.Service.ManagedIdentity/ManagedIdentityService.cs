using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

public sealed class ManagedIdentityService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".managed-identity");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "managedidentity";
    public string Name => "Managed Identity";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ManagedIdentityEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}