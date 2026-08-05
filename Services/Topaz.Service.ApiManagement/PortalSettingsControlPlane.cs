using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class PortalSettingsControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static PortalSettingsControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private static readonly string PortalSettingsSubresourceId = nameof(Subresources.PortalSettings).ToLowerInvariant();
    private static readonly string PortalSettingsETagSubresourceId = "portalsettings-etag";
    
    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }
    
    public ControlPlaneOperationResult<PortalSignInSettingsResource> GetSignInSettings(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PortalSignInSettingsResource>(subscriptionIdentifier,
            resourceGroupIdentifier, "signin", apimName, PortalSettingsSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Success, existing)
            : new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                "SignInSettings not found", "PortalSettingsNotFound");
    }

    public ControlPlaneOperationResult<PortalSignInSettingsResource> CreateOrUpdateSignInSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdatePortalSignInSettingsRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetSignInSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            var signInSettings = new PortalSignInSettingsResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                PortalSignInSettingsResource.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
                PortalSettingsSubresourceId, signInSettings);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
                PortalSettingsETagSubresourceId, signInSettings.ETag);

            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Created, signInSettings);
        }

        // As per API docs, If-Match is required for CreateOrUpdateSignInSettings operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Updated, existing.Resource);
    }
}