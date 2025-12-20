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
}