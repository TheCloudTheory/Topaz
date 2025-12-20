using Topaz.ResourceManager;
using Topaz.Service.ManagedIdentity.Models;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

internal sealed class ManagedIdentityControlPlane(
    ManagedIdentityResourceProvider provider,
    ResourceGroupControlPlane resourceGroupControlPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger
) : IControlPlane
{
    public ControlPlaneOperationResult<ManagedIdentityResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string managedIdentityName, CreateUpdateManagedIdentityRequest request)
    {
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.Failed, null,
                subscriptionOperation.Reason,
                subscriptionOperation.Code);
        }
        
        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.Failed, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }
        
        var resource = new ManagedIdentityResource(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName, request.Location, request.Tags, ManagedIdentityResourceProperties.From(request.Properties!));

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName, resource);

        return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.Created, resource, null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        var identity = resource.As<ManagedIdentityResource, ManagedIdentityResourceProperties>();
        if (identity == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Managed Identity instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdate(identity.GetSubscription(), identity.GetResourceGroup(), identity.Name,
            new CreateUpdateManagedIdentityRequest
            {
                Location = identity.Location,
                Properties = new CreateUpdateManagedIdentityRequest.ManagedIdentityProperties
                {
                    IsolationScope = identity.Properties.IsolationScope
                }
            });

        return result.Result;
    }

    public ControlPlaneOperationResult<ManagedIdentityResource[]> ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource[]>(OperationResult.NotFound, null, subscription.Reason, subscription.Code);
        }
        
        var resources = provider.ListAs<ManagedIdentityResource>(subscriptionIdentifier, null, null, 8);
        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));
        
        return new ControlPlaneOperationResult<ManagedIdentityResource[]>(OperationResult.Success, filteredResources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<ManagedIdentityResource[]> ListByResourceGroup(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource[]>(OperationResult.NotFound, null, subscription.Reason, subscription.Code);
        }

        var resourceGroup = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Resource == null || resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource[]>(OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }
        
        var resources = provider.ListAs<ManagedIdentityResource>(subscriptionIdentifier, resourceGroupIdentifier);
        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier) && resource.IsInResourceGroup(resourceGroupIdentifier));
        
        return new ControlPlaneOperationResult<ManagedIdentityResource[]>(OperationResult.Success, filteredResources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<ManagedIdentityResource> Get(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string managedIdentityName)
    {
        var resource = provider.GetAs<ManagedIdentityResource>(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName);
        return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.Success, resource, null, null);
    }
}