using Topaz.Service.Shared;

namespace Topaz.Service.ResourceManager;

// Used only as a generic type parameter for ManagementGroupDeploymentResourceProvider.
// Management-group-scope deployment endpoints are registered in ResourceManagerService.
internal sealed class ManagementGroupDeploymentService : IServiceDefinition
{
    public static string UniqueName => "management-group-deployment";
    public string Name => "Management Group Deployment";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => ".management-group";
    public static IReadOnlyCollection<string> Subresources => [];
    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];
    public void Bootstrap() { }
}
