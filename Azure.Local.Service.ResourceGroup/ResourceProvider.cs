using System.Text.Json;
using Azure.Local.Shared;

namespace Azure.Local.Service.ResourceGroup;

internal sealed class ResourceProvider(ILogger logger)
{
    private readonly ILogger logger = logger;

    internal Models.ResourceGroup Create(string name, string location)
    {
        var fileName = $"{name}.json";
        var resourceGroupPath = Path.Combine(ResourceGroupService.LocalDirectoryPath, fileName);
        if(File.Exists(resourceGroupPath)) 
        {
            this.logger.LogDebug($"The resource group '{name}' already exists, no changes applied.");
            return new Models.ResourceGroup(name, location);
        }

        this.logger.LogDebug($"Creating storage account '{name}'.");

        var model = new Models.ResourceGroup(name, location);
        var data = JsonSerializer.Serialize(model);
        
        File.WriteAllText(resourceGroupPath, data);

        return new Models.ResourceGroup(name, location);
    }

    internal void Delete(string name)
    {
        var fileName = $"{name}.json";
        var resourceGroupPath = Path.Combine(ResourceGroupService.LocalDirectoryPath, fileName);
        if(File.Exists(resourceGroupPath) == false) 
        {
            this.logger.LogDebug($"The resource group '{name}' does not exists, no changes applied.");
            return;
        }

        this.logger.LogDebug($"Deleting resource group '{name}'.");
        File.Delete(resourceGroupPath);

        return;
    }
}
