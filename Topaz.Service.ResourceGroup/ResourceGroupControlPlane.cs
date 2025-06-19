using System.Net;
using System.Text.Json;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

internal sealed class ResourceGroupControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public Models.ResourceGroup Get(string name)
    {
        var data = provider.Get(name);
        var model = JsonSerializer.Deserialize<Models.ResourceGroup>(data, GlobalSettings.JsonOptions);

        return model!;
    }

    public (OperationResult result, ResourceGroupResource resource) Create(string resourceGroupName, string subscriptionId, string location)
    {
        var model = new ResourceGroupResource(subscriptionId, resourceGroupName, location, new ResourceGroupProperties());

        provider.Create(resourceGroupName, model);

        return (OperationResult.Created, model);
    }

    public (OperationResult result, ResourceGroupResource resource) CreateOrUpdate(string resourceGroupName, string subscriptionId, CreateOrUpdateResourceGroupRequest request)
    {
        var resource = provider.GetAs<ResourceGroupResource>(resourceGroupName);
        if (resource != null)
        {
            logger.LogDebug($"Resource group {resourceGroupName} already exists.");
            return (OperationResult.Updated, resource);
        }
        
        logger.LogDebug($"Creating resource group {resourceGroupName} because it doesn't exist.");
        var newResource = new ResourceGroupResource(subscriptionId, resourceGroupName, request.Location!, new ResourceGroupProperties());
        provider.CreateOrUpdate(resourceGroupName, newResource);
            
        return (OperationResult.Created, newResource);
    }
    
    public (OperationResult result, ResourceGroupResource[] resources) List(string subscriptionId)
    {
        var resources = provider.List();
        var groups = resources
            .Select(r => JsonSerializer.Deserialize<ResourceGroupResource>(r, GlobalSettings.JsonOptions)!)
            .Where(g => g.Id.Contains(subscriptionId)).ToArray();
        
        return (OperationResult.Success,  groups);
    }
}
