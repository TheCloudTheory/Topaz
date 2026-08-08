using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
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

    private readonly SubscriptionControlPlane _subscriptionControlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);
    
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

    public ControlPlaneOperationResult<ApiManagementServiceFullResource> CreateOrUpdate(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, CreateOrUpdateApiManagementServiceRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if(existing.Result == OperationResult.NotFound)
        {
            var apim = new ApiManagementServiceFullResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                request.Location!, request.Tags, request.Sku, ApiManagementServiceResourceProperties.From(request));

            if (!apim.Validate<ApiManagementServiceFullResource>().IsValid)
            {
                return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                    OperationResult.BadRequest, null, apim.Validate<ApiManagementServiceFullResource>().Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, apimName, apim, createOperation: true);
            
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                OperationResult.Created, apim);
        }
        
        existing.Resource!.UpdateFromRequest(request);
        
        if (!existing.Resource.Validate<ApiManagementServiceFullResource>().IsValid)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                OperationResult.BadRequest, null, existing.Resource.Validate<ApiManagementServiceFullResource>().Error, "InvalidRequest");
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, apimName, existing.Resource, createOperation: false);
        return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
            OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<ApiManagementServiceFullResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string name, bool ignoreSoftDeleted = false)
    {
        var resource = provider.GetAs<ApiManagementServiceFullResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        return resource == null || (GlobalDnsEntries.IsSoftDeleted(ApiManagementService.UniqueName, name) && !ignoreSoftDeleted)
            ? new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<ApiManagementServiceFullResource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult<ApiManagementServiceNameAvailabilityResult> CheckNameAvailability(
        SubscriptionIdentifier subscriptionIdentifier, CheckNameAvailabilityRequest request)
    {
        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceNameAvailabilityResult>(
                subscriptionOperation.Result, null, subscriptionOperation.Reason, subscriptionOperation.Code);
        }
        
        var existingEntry = GlobalDnsEntries.GetEntry(ApiManagementService.UniqueName, request.Name!);
        if (existingEntry != null)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceNameAvailabilityResult>(OperationResult.Success,
                ApiManagementServiceNameAvailabilityResult.ForAlreadyExists());
        }

        if(ApiManagementServiceResource.CheckIfNameIsValid(request.Name!))
        {
            return new ControlPlaneOperationResult<ApiManagementServiceNameAvailabilityResult>(
                OperationResult.Success, ApiManagementServiceNameAvailabilityResult.ForInvalidName());
        }

        return new ControlPlaneOperationResult<ApiManagementServiceNameAvailabilityResult>(
            OperationResult.Success, ApiManagementServiceNameAvailabilityResult.ForValidName());
    }

    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string name)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, name);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        existing.Resource!.DeletionDate = DateTime.UtcNow;
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, existing.Resource);
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, name, softDelete: true);

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ApiManagementServiceFullResource[]> ListByResourceGroup(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource[]>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }

        var existing = provider
            .ListAs<ApiManagementServiceFullResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8)
            .Where(apim => !GlobalDnsEntries.IsSoftDeleted(ApiManagementService.UniqueName, apim.Name));

        return new ControlPlaneOperationResult<ApiManagementServiceFullResource[]>(OperationResult.Success, [.. existing]);
    }

    public ControlPlaneOperationResult<ApiManagementServiceFullResource[]> List(SubscriptionIdentifier subscriptionIdentifier)
    {
        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource[]>(
                subscriptionOperation.Result, null, subscriptionOperation.Reason, subscriptionOperation.Code);
        }
        
        var resources = provider.ListAs<ApiManagementServiceFullResource>(subscriptionIdentifier, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .Where(apim => !GlobalDnsEntries.IsSoftDeleted(ApiManagementService.UniqueName, apim.Name))
            .ToArray();
        
        return new ControlPlaneOperationResult<ApiManagementServiceFullResource[]>(OperationResult.Success, resources);
    }

    public ControlPlaneOperationResult<ApiManagementServiceFullResource> Update(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string name,
        CreateOrUpdateApiManagementServiceRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                OperationResult.NotFound, null, resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }
            
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, name);
        if(existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }
        
        existing.Resource!.UpdateFromRequest(request);
        
        if (!existing.Resource.Validate<ApiManagementServiceFullResource>().IsValid)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                OperationResult.BadRequest, null, existing.Resource.Validate<ApiManagementServiceFullResource>().Error, "InvalidRequest");
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, existing.Resource);
        return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
            OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<ApiManagementServiceFullResource> GetDeletedService(SubscriptionIdentifier subscriptionIdentifier, string apimName)
    {
        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<ApiManagementServiceFullResource>(
                subscriptionOperation.Result, null, subscriptionOperation.Reason, subscriptionOperation.Code);
        }

        var deleted = ListDeletedBySubscription(subscriptionIdentifier);
        var apim = deleted.Resource!.SingleOrDefault(apim => apim.Name == apimName);

        return apim == null
            ? new ControlPlaneOperationResult<ApiManagementServiceFullResource>(OperationResult.NotFound, null)
            : new ControlPlaneOperationResult<ApiManagementServiceFullResource>(OperationResult.Success, apim);
    }
    
    private ControlPlaneOperationResult<ApiManagementServiceFullResource[]> ListDeletedBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var apims = ListBySubscription(subscriptionIdentifier);
        var filteredResources = apims.Resource!.Where(apim =>
            GlobalDnsEntries.IsSoftDeleted(ApiManagementService.UniqueName, apim.Name));

        return new ControlPlaneOperationResult<ApiManagementServiceFullResource[]>(OperationResult.Success,
            [.. filteredResources]);
    }
    
    private ControlPlaneOperationResult<ApiManagementServiceFullResource[]> ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<ApiManagementServiceFullResource>(subscriptionIdentifier, null, null, 8);
        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));
        
        return new ControlPlaneOperationResult<ApiManagementServiceFullResource[]>(OperationResult.Success,
            [.. filteredResources]);
    }
}