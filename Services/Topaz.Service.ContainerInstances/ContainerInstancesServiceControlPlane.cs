using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerInstances.Models;
using Topaz.Service.ContainerInstances.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ContainerInstances;

internal sealed class ContainerInstancesServiceControlPlane(
    Pipeline eventPipeline,
    ContainerInstancesResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "ContainerInstances resource '{0}' could not be found.";
    
    private readonly SubscriptionControlPlane _subscriptionControlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);

    public static ContainerInstancesServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ContainerInstancesResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var aci = resource.As<ContainerInstancesServiceResource, ContainerInstancesServiceResourceProperties>();
        if (aci == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Azure Container Instances instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(aci.Location))
        {
            logger.LogError($"Azure Container Instances resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(aci.GetSubscription(), aci.GetResourceGroup(), aci.Name, CreateOrUpdateContainerGroupRequest.From(aci));
            return result.Result is OperationResult.Created or OperationResult.Updated
                ? OperationResult.Success
                : OperationResult.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<ContainerInstancesServiceResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string containerGroupName, CreateOrUpdateContainerGroupRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            var apim = new ContainerInstancesServiceResource(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName,
                request.Location!, request.Tags, request.Sku, ContainerInstancesServiceResourceProperties.From(request));

            if (!apim.Validate<ContainerInstancesServiceResource>().IsValid)
            {
                return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                    OperationResult.BadRequest, null, apim.Validate<ContainerInstancesServiceResource>().Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName, apim, createOperation: true);
            
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.Created, apim);
        }
        
        existing.Resource!.UpdateFromRequest(request);
        
        if (!existing.Resource.Validate<ContainerInstancesServiceResource>().IsValid)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.BadRequest, null, existing.Resource.Validate<ContainerInstancesServiceResource>().Error, "InvalidRequest");
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName, existing.Resource, createOperation: false);
        return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
            OperationResult.Updated, existing.Resource);
    }
    
    public ControlPlaneOperationResult<ContainerInstancesServiceResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName)
    {
        var resource = provider.GetAs<ContainerInstancesServiceResource>(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        return resource == null
            ? new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, containerGroupName), NotFoundCode)
            : new ControlPlaneOperationResult<ContainerInstancesServiceResource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult<ContainerInstancesServiceResource> Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.NotFound, null, existing.Reason, existing.Code);
        }
        
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);

        return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(OperationResult.Deleted, existing.Resource);
    }

    public ControlPlaneOperationResult<ContainerInstancesServiceResource[]> List(SubscriptionIdentifier subscriptionIdentifier)
    {
        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource[]>(
                subscriptionOperation.Result, null, subscriptionOperation.Reason, subscriptionOperation.Code);
        }
        
        var resources = provider.ListAs<ContainerInstancesServiceResource>(subscriptionIdentifier, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();
        
        return new ControlPlaneOperationResult<ContainerInstancesServiceResource[]>(OperationResult.Success, resources);
    }

    public ControlPlaneOperationResult<ContainerInstancesServiceResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource[]>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }

        var resources = provider
            .ListAs<ContainerInstancesServiceResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: 8)
            .ToArray();

        return new ControlPlaneOperationResult<ContainerInstancesServiceResource[]>(OperationResult.Success, resources);
    }

    public ControlPlaneOperationResult Restart(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName)
    {
        var existing= Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult Start(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName)
    {
        var existing= Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        return new ControlPlaneOperationResult(OperationResult.Success);
    }
    
    public ControlPlaneOperationResult Stop(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName)
    {
        var existing= Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        return new ControlPlaneOperationResult(OperationResult.Success);
    }
    
    public ControlPlaneOperationResult<ContainerInstancesServiceResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName, CreateOrUpdateContainerGroupRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }
        
        existing.Resource!.UpdateFromRequest(request);
        
        if (!existing.Resource.Validate<ContainerInstancesServiceResource>().IsValid)
        {
            return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
                OperationResult.BadRequest, null, existing.Resource.Validate<ContainerInstancesServiceResource>().Error, "InvalidRequest");
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName, existing.Resource);
        return new ControlPlaneOperationResult<ContainerInstancesServiceResource>(
            OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<string> ListLogs(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerGroupName, string containerName)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, containerGroupName);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }
        
        return new ControlPlaneOperationResult<string>(OperationResult.Success, "Logs streaming is not yet available in Topaz.");
    }
}