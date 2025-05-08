using System.Net;
using System.Text.Json;
using Azure.Local.Service.ResourceGroup.Models;
using Azure.Local.Service.Shared;
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

        this.logger.LogDebug($"Creating resource group '{name}'.");

        var model = new Models.ResourceGroup(name, location);
        var data = JsonSerializer.Serialize(model);
        
        File.WriteAllText(resourceGroupPath, data);

        return model;
    }

    internal (Models.ResourceGroup data, HttpStatusCode code) CreateOrUpdate(string name, Stream input)
    {
        var fileName = $"{name}.json";
        var resourceGroupPath = Path.Combine(ResourceGroupService.LocalDirectoryPath, fileName);
        if(File.Exists(resourceGroupPath)) 
        {
            this.logger.LogDebug($"The resource group '{name}' already exists, no changes applied.");

            var content = File.ReadAllText(resourceGroupPath);
            var data = JsonSerializer.Deserialize<Models.ResourceGroup>(content);

            return (data!, code: HttpStatusCode.OK);
        }

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateRequest>(rawContent, GlobalSettings.JsonOptions);
        var newData = new Models.ResourceGroup(name, request!.Location!);
        
        File.WriteAllText(resourceGroupPath, JsonSerializer.Serialize(newData, GlobalSettings.JsonOptions));

        return (data: newData, code: HttpStatusCode.Created);
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

    internal (Models.ResourceGroup? data, HttpStatusCode code) Get(string name)
    {
        var fileName = $"{name}.json";
        var resourceGroupPath = Path.Combine(ResourceGroupService.LocalDirectoryPath, fileName);
        if(File.Exists(resourceGroupPath) == false) 
        {
            this.logger.LogDebug($"The resource group '{name}' does not exist.");
            return (null, HttpStatusCode.NotFound);
        }

        var content = File.ReadAllText(resourceGroupPath);
        var data = JsonSerializer.Deserialize<Models.ResourceGroup>(content, GlobalSettings.JsonOptions);

        return (data!, code: HttpStatusCode.OK);
    }
}
