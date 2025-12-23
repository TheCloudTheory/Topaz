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
    private const string ManagedIdentityNotFoundCode = "ManagedIdentityNotFound";
    private const string ManagedIdentityNotFoundMessageTemplate =
        "Managed Identity '{0}' could not be found";
    
    public static ManagedIdentityControlPlane New(ITopazLogger logger) => new(
        new ManagedIdentityResourceProvider(logger),
        ResourceGroupControlPlane.New(logger),
        SubscriptionControlPlane.New(logger),
        logger);
    
    public ControlPlaneOperationResult<ManagedIdentityResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ManagedIdentityIdentifier managedIdentityIdentifier, CreateUpdateManagedIdentityRequest request)
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

        var managedIdentityOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        var isCreateOperation = managedIdentityOperation.Result == OperationResult.NotFound;
        var resource = isCreateOperation
            ? new ManagedIdentityResource(subscriptionIdentifier, resourceGroupIdentifier,
                managedIdentityIdentifier.Value, request.Location, request.Tags,
                ManagedIdentityResourceProperties.From(request.Properties))
            : managedIdentityOperation.Resource!;

        if (!isCreateOperation)
        {
            resource.UpdateFrom(request);
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier.Value, resource);

        return new ControlPlaneOperationResult<ManagedIdentityResource>(isCreateOperation ? OperationResult.Created : OperationResult.Updated, resource, null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        var identity = resource.As<ManagedIdentityResource, ManagedIdentityResourceProperties>();
        if (identity == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Managed Identity instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdate(identity.GetSubscription(), identity.GetResourceGroup(), ManagedIdentityIdentifier.From(identity.Name),
            new CreateUpdateManagedIdentityRequest
            {
                Location = identity.Location,
                Tags = identity.Tags,
                Properties = new CreateUpdateManagedIdentityRequest.ManagedIdentityProperties
                {
                    IsolationScope = identity.Properties?.IsolationScope
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
        
        var resources = provider.ListAs<ManagedIdentityResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier) && resource.IsInResourceGroup(resourceGroupIdentifier));
        
        return new ControlPlaneOperationResult<ManagedIdentityResource[]>(OperationResult.Success, filteredResources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<ManagedIdentityResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ManagedIdentityIdentifier managedIdentityIdentifier)
    {
        var resource = provider.GetAs<ManagedIdentityResource>(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier.Value);
        return resource == null ? 
            new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.NotFound, null, string.Format(ManagedIdentityNotFoundMessageTemplate, managedIdentityIdentifier), ManagedIdentityNotFoundCode) 
            : new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, ManagedIdentityIdentifier managedIdentityIdentifier)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, subscription.Reason, subscription.Code);
        }

        var resourceGroup = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Resource == null || resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, resourceGroup.Reason, resourceGroup.Code);
        }
        
        var resource =
            provider.GetAs<ManagedIdentityResource>(subscriptionIdentifier, resourceGroupIdentifier,
                managedIdentityIdentifier.Value);

        if (resource == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                string.Format(ManagedIdentityNotFoundMessageTemplate, managedIdentityIdentifier),
                ManagedIdentityNotFoundCode);
        }
        
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier.Value);
        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<ManagedIdentityResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ManagedIdentityIdentifier managedIdentityIdentifier,
        CreateUpdateManagedIdentityRequest request)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.NotFound, null, subscription.Reason, subscription.Code);
        }

        var resourceGroup = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Resource == null || resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }
        
        var resource =
            provider.GetAs<ManagedIdentityResource>(subscriptionIdentifier, resourceGroupIdentifier,
                managedIdentityIdentifier.Value);

        if (resource == null)
        {
            return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.NotFound, null,
                string.Format(ManagedIdentityNotFoundMessageTemplate, managedIdentityIdentifier),
                ManagedIdentityNotFoundCode);
        }

        resource.UpdateFrom(request);
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier.Value, resource);
        return new ControlPlaneOperationResult<ManagedIdentityResource>(OperationResult.Updated,
            resource, null, null);
    }
}