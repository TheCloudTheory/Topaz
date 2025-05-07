using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.ResourceGroup;

public sealed class ResourceGroupService : IServiceDefinition
{
    internal const string LocalDirectoryPath = ".resource-groups";
    private readonly ILogger logger;

    public string Name => "Resource Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceGroupEndpoint(this.logger)
    ];

    public ResourceGroupService(ILogger logger)
    {
        this.logger = logger;
        InitializeResourceGroupService();
    }

    private void InitializeResourceGroupService()
    {
        this.logger.LogDebug("Attempting to create resource groups directory...");

        if(Directory.Exists(LocalDirectoryPath) == false)
        {
            Directory.CreateDirectory(LocalDirectoryPath);
            this.logger.LogDebug("Local resource groups directory created.");
        }
        else
        {
            this.logger.LogDebug("Attempting to create resource groups directory - skipped.");
        }
    }
}
