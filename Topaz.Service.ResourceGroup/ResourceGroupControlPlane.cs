using System.Text.Json;
using Azure.Core;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupControlPlane(ResourceGroupResourceProvider groupResourceProvider, ITopazLogger logger)
{
    public (OperationResult result, ResourceGroupResource? resource) Get(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resource = groupResourceProvider.GetAs<ResourceGroupResource>(subscriptionIdentifier, resourceGroupIdentifier);

        if (resource != null && !resource.IsInSubscription(subscriptionIdentifier))
        {
            return (OperationResult.NotFound, null);
        }
        
        return resource == null ? 
            (OperationResult.NotFound,  null) : 
            (OperationResult.Success, resource);
    }

    public (OperationResult result, ResourceGroupResource resource) Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location)
    {
        var model = new ResourceGroupResource(subscriptionIdentifier, resourceGroupIdentifier.Value, location, new ResourceGroupProperties());

        groupResourceProvider.Create(subscriptionIdentifier, resourceGroupIdentifier, null, model);

        return (OperationResult.Created, model);
    }

    public (OperationResult result, ResourceGroupResource resource) CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        CreateOrUpdateResourceGroupRequest request)
    {
        var resource = groupResourceProvider.GetAs<ResourceGroupResource>(subscriptionIdentifier, resourceGroupIdentifier);
        if (resource != null)
        {
            logger.LogDebug($"Resource group {resourceGroupIdentifier} already exists.");
            return (OperationResult.Updated, resource);
        }
        
        logger.LogDebug($"Creating resource group {resourceGroupIdentifier} because it doesn't exist.");
        var newResource = new ResourceGroupResource(subscriptionIdentifier, resourceGroupIdentifier.Value, request.Location!, new ResourceGroupProperties());
        groupResourceProvider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, null, newResource);
            
        return (OperationResult.Created, newResource);
    }
    
    public (OperationResult result, ResourceGroupResource[] resources) List(SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = groupResourceProvider.List(subscriptionIdentifier, null);
        var groups = resources
            .Select(r => JsonSerializer.Deserialize<ResourceGroupResource>(r, GlobalSettings.JsonOptions)!)
            .Where(g => g.Id.Contains(subscriptionIdentifier.Value.ToString())).ToArray();
        
        return (OperationResult.Success,  groups);
    }

    public OperationResult Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        groupResourceProvider.Delete(subscriptionIdentifier, resourceGroupIdentifier, null);
        return OperationResult.Deleted;
    }
}
