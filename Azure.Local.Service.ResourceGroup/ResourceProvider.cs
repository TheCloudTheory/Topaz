using Azure.Local.Shared;

namespace Azure.Local.Service.ResourceGroup;

internal sealed class ResourceProvider(ILogger logger)
{
    private readonly ILogger logger = logger;

    internal Models.ResourceGroup Create(string name, string location)
    {
        var fileName = $"{name}_{location}.json";
        var resourceGroupPath = Path.Combine(ResourceGroupService.LocalDirectoryPath, fileName);
        if(File.Exists(resourceGroupPath)) 
        {
            this.logger.LogDebug($"The resource group '{name}' already exists, no changes applied.");
            return new Models.ResourceGroup(name, location);
        }

        this.logger.LogDebug($"Creating storage account '{name}'.");
        Directory.CreateDirectory(resourceGroupPath);

        return new Models.ResourceGroup(name, location);
    }
}
