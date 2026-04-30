using Topaz.Service.Shared;

namespace Topaz.Service.ResourceManager;

// Used only as a generic type parameter for TenantDeploymentResourceProvider.
// Tenant-scope deployment endpoints are registered in ResourceManagerService.
internal sealed class TenantDeploymentService : IServiceDefinition
{
    public static string UniqueName => "tenant-deployment";
    public string Name => "Tenant Deployment";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => ".tenant";
    public static IReadOnlyCollection<string> Subresources => [];
    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];
    public void Bootstrap() { }
}
