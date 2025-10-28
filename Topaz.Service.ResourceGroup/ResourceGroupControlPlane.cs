using System.Text.Json;
using Azure.Core;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

internal sealed class ResourceGroupControlPlane(ResourceGroupResourceProvider groupResourceProvider, SubscriptionControlPlane subscriptionControlPlane, ITopazLogger logger)
{
    private const string ResourceGroupNotFoundMessageTemplate =
        "Resource group '{0}' could not be found";

    private const string ResourceGroupNotFoundMessageCode = "ResourceGroupNotFound";
    
    public ControlPlaneOperationResult<ResourceGroupResource> Get(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resource = groupResourceProvider.GetAs<ResourceGroupResource>(subscriptionIdentifier, resourceGroupIdentifier);
        if (resource != null && !resource.IsInSubscription(subscriptionIdentifier))
        {
            return new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.NotFound, null,
                string.Format(ResourceGroupNotFoundMessageTemplate, resourceGroupIdentifier.Value),
                ResourceGroupNotFoundMessageCode);
        }
        
        return resource == null ? 
            new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.NotFound, null,
                string.Format(ResourceGroupNotFoundMessageTemplate, resourceGroupIdentifier.Value),
                ResourceGroupNotFoundMessageCode) : 
            new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.Success, resource,
                null,
                null);
    }

    public ControlPlaneOperationResult<ResourceGroupResource> Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location)
    {
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound || subscriptionOperation.Resource == null)
        {
            return new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.Failed, null,
                subscriptionOperation.Reason, subscriptionOperation.Code);
        }
        
        var model = new ResourceGroupResource(subscriptionIdentifier, resourceGroupIdentifier.Value, location, new ResourceGroupProperties());

        groupResourceProvider.Create(subscriptionIdentifier, resourceGroupIdentifier, null, model);

        return new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.Created, model, null, null);
    }

    public ControlPlaneOperationResult<ResourceGroupResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        CreateOrUpdateResourceGroupRequest request)
    {
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound || subscriptionOperation.Resource == null)
        {
            return new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.Failed, null,
                subscriptionOperation.Reason, subscriptionOperation.Code);
        }
        
        var resource = groupResourceProvider.GetAs<ResourceGroupResource>(subscriptionIdentifier, resourceGroupIdentifier);
        if (resource != null)
        {
            logger.LogDebug($"Resource group {resourceGroupIdentifier} already exists.");
            return new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.Updated, resource, null, null);
        }
        
        logger.LogDebug($"Creating resource group {resourceGroupIdentifier} because it doesn't exist.");
        var newResource = new ResourceGroupResource(subscriptionIdentifier, resourceGroupIdentifier.Value, request.Location!, new ResourceGroupProperties());
        groupResourceProvider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, null, newResource);
            
        return new ControlPlaneOperationResult<ResourceGroupResource>(OperationResult.Created, newResource, null, null);
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
