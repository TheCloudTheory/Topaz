using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class TenantControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string AccessId = "access";
    
    public static TenantControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private static readonly string TenantSubresourceId = nameof(Subresources.Tenants).ToLowerInvariant();
    private static readonly string TenantETagSubresourceId = "tenant-etag";
    
    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);

    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }
    
    public ControlPlaneOperationResult<TenantAccessResource> GetTenantAccessSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<TenantAccessResource>(subscriptionIdentifier,
            resourceGroupIdentifier, AccessId, apimName, TenantSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.Success, existing.ForGetOperation())
            : new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.Success, TenantAccessResource.Default);
    }

    public ControlPlaneOperationResult<TenantAccessResource> CreateOrUpdateTenantAccess(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdateTenantAccessRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetTenantAccessSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound || existing.Resource?.IsDefault == true)
        {
            var resource = new TenantAccessResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                TenantAccessResource.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, AccessId, apimName,
                TenantSubresourceId, resource);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, AccessId, apimName,
                TenantETagSubresourceId, resource.ETag);

            return new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.Created, resource);
        }

        // As per API docs, If-Match is required for CreateOrUpdateSignInSettings operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, AccessId, apimName,
            TenantSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, AccessId, apimName,
            TenantETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<TenantAccessResource>(OperationResult.Updated, existing.Resource);
    }
    
    public ControlPlaneOperationResult<AccessInformationSecretsContract> ListTenantAccessSecrets(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AccessInformationSecretsContract>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetTenantAccessSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AccessInformationSecretsContract>(OperationResult.NotFound, null, existing.Reason,
                existing.Code);
        }

        var result = AccessInformationSecretsContract.From(existing.Resource!);
        return new ControlPlaneOperationResult<AccessInformationSecretsContract>(OperationResult.Success, result);
    }
}