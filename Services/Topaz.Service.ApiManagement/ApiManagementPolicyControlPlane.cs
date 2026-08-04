using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementPolicyControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static ApiManagementPolicyControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private static readonly string PolicySubresourceId = nameof(Subresources.Policies).ToLowerInvariant();
    private static readonly string PolicyEtagSubresourceId = "policies-etag";
    
    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }
    
    public ControlPlaneOperationResult<PolicyContractResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string backendId, CreateOrUpdatePolicyRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId);
        (bool IsValid, string? Error) validationResult;
        if (existing.Result == OperationResult.NotFound)
        {
            var api = new PolicyContractResource(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId,
                PolicyContractResourceProperties.From(request));
            
            validationResult = api.Validate<ApiContractResource>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.BadRequest, null,
                    validationResult.Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
                PolicySubresourceId, api);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
                PolicyEtagSubresourceId, api.ETag);

            return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.Created, api);
        }

        // As per API docs, If-Match is required for CreateOrUpdate operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        validationResult = existing.Resource!.Validate<PolicyContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            PolicySubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            PolicyEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<PolicyContractResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string backendId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PolicyContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            backendId, apimName, PolicySubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.NotFound, null,
                $"Backend {backendId} not found", "BackendNotFound");
        }
        
        return new ControlPlaneOperationResult<PolicyContractResource>(OperationResult.Success, existing);
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
            apimName, PolicyEtagSubresourceId);

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
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName, PolicySubresourceId);
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName, PolicyEtagSubresourceId);

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
            apimName, PolicyEtagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }
    
    public ControlPlaneOperationResult<PolicyContractResource[]> ListByService(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PolicyContractResource[]>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.ListSubresourcesAs<PolicyContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            apimName, PolicySubresourceId);

        return new ControlPlaneOperationResult<PolicyContractResource[]>(OperationResult.Success, existing);
    }
}