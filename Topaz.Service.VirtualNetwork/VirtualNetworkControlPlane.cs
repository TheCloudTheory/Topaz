using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Service.VirtualNetwork.Models;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class VirtualNetworkControlPlane(VirtualNetworkResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    private const string VirtualNetworkNotFoundCode = "VirtualNetworkNotFound";
    private const string VirtualNetworkNotFoundMessageTemplate =
        "Virtual network '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        return OperationResult.Success;
    }

    public ControlPlaneOperationResult<VirtualNetworkResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string virtualNetworkName)
    {
        var resource =
            provider.GetAs<VirtualNetworkResource>(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult<VirtualNetworkResource>(OperationResult.NotFound, null,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName), VirtualNetworkNotFoundCode);
        }
        
        return new ControlPlaneOperationResult<VirtualNetworkResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<VirtualNetworkResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName, CreateOrUpdateVirtualNetworkRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<VirtualNetworkResource>(OperationResult.NotFound, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var properties = VirtualNetworkResourceProperties.FromRequest(request);
        var resource = new VirtualNetworkResource(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, resourceGroupOperation.Resource!.Location, properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, resource);
        
        return new ControlPlaneOperationResult<VirtualNetworkResource>(OperationResult.Created, resource, null, null);
    }
}