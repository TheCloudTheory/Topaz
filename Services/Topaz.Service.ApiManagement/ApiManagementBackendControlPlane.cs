using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementBackendControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static ApiManagementBackendControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private static readonly string BackendSubresourceId = nameof(Subresources.Backends).ToLowerInvariant();
    private static readonly string BackendEtagSubresourceId = "backends-etag";

    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }
    
    public ControlPlaneOperationResult<BackendContractResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string backendId, CreateOrUpdateBackendRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId);
        (bool IsValid, string? Error) validationResult;
        if (existing.Result == OperationResult.NotFound)
        {
            var api = new BackendContractResource(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId,
                BackendContractResourceProperties.From(request));
            
            validationResult = api.Validate<ApiContractResource>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                    validationResult.Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
                BackendSubresourceId, api);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
                BackendEtagSubresourceId, api.ETag);

            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Created, api);
        }

        // As per API docs, If-Match is required for CreateOrUpdateSignInSettings operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        validationResult = existing.Resource!.Validate<ApiContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            BackendSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            BackendEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<BackendContractResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string backendId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<BackendContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            backendId, apimName, BackendSubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                $"Backend {backendId} not found", "BackendNotFound");
        }
        
        return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Success, existing);
    }
    
    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string backendId, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                apimOperation.Reason, apimOperation.Code);
        }
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        // As per docs, If-Match header must be present for delete operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult(OperationResult.BadRequest,
                "If-Match is required for delete requests.", "MissingIfMatchHeader");
        }
        
        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, backendId,
            apimName, BackendEtagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Delete), "API Management backend is missing ETag value");
            
            return new ControlPlaneOperationResult(OperationResult.Failed, "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName, BackendSubresourceId);
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName, BackendEtagSubresourceId);

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }
    
    public ControlPlaneOperationResult<string> GetEntityTag(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string backendId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, backendId,
            apimName, BackendEtagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }
    
    public ControlPlaneOperationResult<BackendContractResource[]> ListByService(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource[]>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.ListSubresourcesAs<BackendContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            apimName, BackendSubresourceId);

        return new ControlPlaneOperationResult<BackendContractResource[]>(OperationResult.Success, existing);
    }
    
    public ControlPlaneOperationResult<BackendContractResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string backendId, CreateOrUpdateBackendRequest request,
        string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        // As per API docs, If-Match is required for Update operation,
        // and it must match the current ETag (unless it's unconditional update with "*")
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, backendId,
            apimName, BackendEtagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Update), "API Management backend is missing ETag value");
            
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Failed, null, "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Conflict, null,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        var validationResult = existing.Resource!.Validate<BackendContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            BackendSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            BackendEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Updated, existing.Resource);
    }
}