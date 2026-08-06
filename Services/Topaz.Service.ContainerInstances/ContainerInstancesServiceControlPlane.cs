using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerInstances.Models;
using Topaz.Service.ContainerInstances.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerInstances;

internal sealed class ContainerInstancesServiceControlPlane(
    Pipeline eventPipeline,
    ContainerInstancesResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "ContainerInstances resource '{0}' could not be found.";
    
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);

    public static ContainerInstancesServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ContainerInstancesResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        // TODO: replace MyResource / MyResourceProperties with your actual model types
        // var typed = resource.As<MyResource, MyResourceProperties>();
        // if (typed == null)
        // {
        //     logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a ContainerInstances instance.");
        //     return OperationResult.Failed;
        // }

        try
        {
            // TODO: call CreateOrUpdate / other provider methods here
            return OperationResult.Success;
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
}