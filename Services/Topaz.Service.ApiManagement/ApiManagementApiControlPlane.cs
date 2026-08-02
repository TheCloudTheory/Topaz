using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementApiControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static ApiManagementApiControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);

    private static readonly string ApiSubresourceId = nameof(Subresources.Apis).ToLowerInvariant();
    private static readonly string ApiEtagSubresourceId = "apis-etag";
    private static readonly string ApiRevisionSubresourceId = "apis-revision";

    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);

    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }

    public ControlPlaneOperationResult<ApiContractResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string apiId, CreateOrUpdateApiRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        (bool IsValid, string? Error) validationResult;
        if (existing.Result == OperationResult.NotFound)
        {
            var api = new ApiContractResource(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId,
                ApiContractResourceProperties.From(request));
            
            validationResult = api.Validate<ApiContractResource>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                    validationResult.Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
                ApiSubresourceId, api);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
                ApiEtagSubresourceId, api.ETag);

            var revision = ApiRevisionData.From(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId, request);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName, ApiRevisionSubresourceId, revision);

            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Created, api);
        }

        // As per API docs, If-Match is required for CreateOrUpdate operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        validationResult = existing.Resource!.Validate<ApiContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
            ApiSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
            ApiEtagSubresourceId, existing.Resource.ETag);

        var existingRevision = provider.GetSubresourceAs<ApiRevisionData>(subscriptionIdentifier,
            resourceGroupIdentifier, apiId, apimName, ApiRevisionSubresourceId);
        if (existingRevision == null)
        {
            var revision = ApiRevisionData.From(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId, request);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
                ApiRevisionSubresourceId, revision);
        }
        else
        {
            existingRevision.Update(request);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
                ApiRevisionSubresourceId, existingRevision);
        }

        return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<ApiContractResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string apiId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<ApiContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            apiId, apimName, ApiSubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                $"API {apiId} not found", "ApiNotFound");
        }
        
        return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Success, existing);
    }

    public ControlPlaneOperationResult<ApiContractResource[]> ListByService(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource[]>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.ListSubresourcesAs<ApiContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            apimName, ApiSubresourceId);

        return new ControlPlaneOperationResult<ApiContractResource[]>(OperationResult.Success, existing);
    }

    public ControlPlaneOperationResult<ApiContractResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string apiId, CreateOrUpdateApiRequest request,
        string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        // As per API docs, If-Match is required for Update operation,
        // and it must match the current ETag (unless it's unconditional update with "*")
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        var etag = provider.GetSubresourceAs<ApiContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, apiId,
            apimName, ApiEtagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Update), "API Management API is missing ETag value");
            
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Failed, null, "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Conflict, null,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        var validationResult = existing.Resource!.Validate<ApiContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
            ApiSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
            ApiEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<string> GetEntityTag(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string apiId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        var etag = provider.GetSubresourceAs<ApiContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, apiId,
            apimName, ApiEtagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }

    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string apiId, string? ifMatch,
        bool deleteRevisions)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                apimOperation.Reason, apimOperation.Code);
        }
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        // As per docs, If-Match header must be present for delete operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult(OperationResult.BadRequest,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        var etag = provider.GetSubresourceAs<ApiContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, apiId,
            apimName, ApiEtagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Update), "API Management API is missing ETag value");
            
            return new ControlPlaneOperationResult(OperationResult.Failed, "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName, ApiSubresourceId);
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName, ApiEtagSubresourceId);
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName, ApiRevisionSubresourceId);

        if (!deleteRevisions) return new ControlPlaneOperationResult(OperationResult.Deleted);
        
        logger.LogDebug(nameof(ApiManagementApiControlPlane), nameof(Delete), "Deleting all revisions.");
        
        var revisionsToDelete = provider.ListSubresourcesAs<ApiRevisionData>(subscriptionIdentifier,
            resourceGroupIdentifier, apimName, ApiRevisionSubresourceId).Where(revision =>
            revision.ApiId!.Contains(apiId.Split(';')[0]));

        foreach (var revision in revisionsToDelete)
        {
            provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, revision.GetApiId(),
                apimName, ApiRevisionSubresourceId);
        }

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ApiRevisionData[]> ListRevisionsByService(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string apiId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiRevisionData[]>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiRevisionData[]>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        var revisions = provider.ListSubresourcesAs<ApiRevisionData>(subscriptionIdentifier, resourceGroupIdentifier,
            apimName, ApiRevisionSubresourceId);

        return new ControlPlaneOperationResult<ApiRevisionData[]>(OperationResult.Success, revisions);
    }
}