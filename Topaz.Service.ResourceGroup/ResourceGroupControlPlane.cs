using System.Net;
using System.Text.Json;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupControlPlane(ResourceGroupResourceProvider groupResourceProvider, ITopazLogger logger)
{
    public (OperationResult result, ResourceGroupResource? resource) Get(ResourceGroupIdentifier resourceGroup)
    {
        var resource = groupResourceProvider.GetAs<ResourceGroupResource>(resourceGroup.Value);
        return resource == null ? 
            (OperationResult.NotFound,  null) : 
            (OperationResult.Success, resource);
    }

    public (OperationResult result, ResourceGroupResource resource) Create(string resourceGroupName, string subscriptionId, string location)
    {
        var model = new ResourceGroupResource(subscriptionId, resourceGroupName, location, new ResourceGroupProperties());

        groupResourceProvider.Create(resourceGroupName, model);

        return (OperationResult.Created, model);
    }

    public (OperationResult result, ResourceGroupResource resource) CreateOrUpdate(ResourceGroupIdentifier resourceGroup, string subscriptionId, CreateOrUpdateResourceGroupRequest request)
    {
        var resource = groupResourceProvider.GetAs<ResourceGroupResource>(resourceGroup.Value);
        if (resource != null)
        {
            logger.LogDebug($"Resource group {resourceGroup} already exists.");
            return (OperationResult.Updated, resource);
        }
        
        logger.LogDebug($"Creating resource group {resourceGroup} because it doesn't exist.");
        var newResource = new ResourceGroupResource(subscriptionId, resourceGroup.Value, request.Location!, new ResourceGroupProperties());
        groupResourceProvider.CreateOrUpdate(resourceGroup.Value, newResource);
            
        return (OperationResult.Created, newResource);
    }
    
    public (OperationResult result, ResourceGroupResource[] resources) List(string subscriptionId)
    {
        var resources = groupResourceProvider.List();
        var groups = resources
            .Select(r => JsonSerializer.Deserialize<ResourceGroupResource>(r, GlobalSettings.JsonOptions)!)
            .Where(g => g.Id.Contains(subscriptionId)).ToArray();
        
        return (OperationResult.Success,  groups);
    }

    public OperationResult Delete(ResourceGroupIdentifier resourceGroup)
    {
        groupResourceProvider.Delete(resourceGroup.Value);
        return OperationResult.Deleted;
    }
}
