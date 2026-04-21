using Topaz.Service.Shared;
using Topaz.Service.Subscription;

namespace Topaz.Service.ResourceManager;

// Used only as a generic type parameter for SubscriptionDeploymentResourceProvider.
// Subscription-scope deployment endpoints are registered in ResourceManagerService.
public sealed class SubscriptionDeploymentService : IServiceDefinition
{
    public static string UniqueName => "subscription-deployment";
    public string Name => "Subscription Deployment";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".resource-manager");
    public static IReadOnlyCollection<string> Subresources => [];
    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];
    public void Bootstrap() { }
}
