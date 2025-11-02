using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerService : IServiceDefinition
{
    public static string UniqueName => "resourcemanager";
    public string Name => "Resource Manager";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".resource-manager");
    public static IReadOnlyCollection<string> Subresources => [];
    
    private static TemplateDeploymentOrchestrator? _deploymentOrchestrator;
    
    private readonly ITopazLogger _logger;

    public ResourceManagerService(ITopazLogger logger)
    {
        _logger = logger;
        
        _deploymentOrchestrator = new TemplateDeploymentOrchestrator(new ResourceManagerResourceProvider(logger), logger);
        _deploymentOrchestrator.Start();
    }
    
    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ResourceManagerEndpoint(_logger, _deploymentOrchestrator!),
    ];
}