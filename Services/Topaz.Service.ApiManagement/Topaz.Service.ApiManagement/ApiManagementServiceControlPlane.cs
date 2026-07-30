using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using OperationResult = Topaz.Service.Shared.OperationResult;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementServiceControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "ApiManagement resource '{0}' could not be found.";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
    
    public static ApiManagementServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var apim = resource.As<ApiManagementServiceResource, ApiManagementServiceResourceProperties>();
        if (apim == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a ApiManagement instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(apim.Location))
        {
            logger.LogError($"ApiManagement resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(apim.GetSubscription(), apim.GetResourceGroup(), apim.Name, CreateOrUpdateApiManagementServiceRequest.From(apim));
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

    public ControlPlaneOperationResult<ApiManagementServiceResource> CreateOrUpdate(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string name, CreateOrUpdateApiManagementServiceRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceResource>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, name);
        if(existing.Result == OperationResult.NotFound)
        {
            var apim = new ApiManagementServiceResource(subscriptionIdentifier, resourceGroupIdentifier, name,
                request.Location, request.Tags, request.Sku, ApiManagementServiceResourceProperties.From(request));
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, apim);
            
            return new ControlPlaneOperationResult<ApiManagementServiceResource>(
                OperationResult.Created, apim);
        }
        
        existing.Resource!.UpdateFromRequest(request);
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, existing.Resource);
        return new ControlPlaneOperationResult<ApiManagementServiceResource>(
            OperationResult.Updated, existing.Resource);
    }

    private ControlPlaneOperationResult<ApiManagementServiceResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string name)
    {
        var resource = provider.GetAs<ApiManagementServiceResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        return resource == null || GlobalDnsEntries.IsSoftDeleted(ApiManagementService.UniqueName, name)
            ? new ControlPlaneOperationResult<ApiManagementServiceResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<ApiManagementServiceResource>(OperationResult.Success, resource, null, null);
    }
}