using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Service.VirtualNetwork.Models;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class PrivateEndpointControlPlane(
    Pipeline eventPipeline,
    PrivateEndpointResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "PrivateEndpointNotFound";
    private const string NotFoundMessageTemplate = "Private endpoint '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);

    private readonly IpAllocationRegistry _ipAllocationRegistry =
        new(new VirtualNetworkResourceProvider(logger));

    public static PrivateEndpointControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new PrivateEndpointResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var pe = resource.As<PrivateEndpointResource, PrivateEndpointResourceProperties>();
        if (pe == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Private Endpoint instance.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(pe.GetSubscription(), pe.GetResourceGroup(), pe.Name,
                new CreateOrUpdatePrivateEndpointRequest
                {
                    Location = pe.Location,
                    Tags = pe.Tags,
                    Properties = new CreateOrUpdatePrivateEndpointRequest.CreateOrUpdatePrivateEndpointRequestProperties
                    {
                        Subnet = pe.Properties.Subnet,
                        PrivateLinkServiceConnections = pe.Properties.PrivateLinkServiceConnections,
                        CustomDnsConfigs = pe.Properties.CustomDnsConfigs
                    }
                });

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<PrivateEndpointResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<PrivateEndpointResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<PrivateEndpointResource>(
                OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, name), NotFoundCode);
        }

        return new ControlPlaneOperationResult<PrivateEndpointResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<PrivateEndpointResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        CreateOrUpdatePrivateEndpointRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PrivateEndpointResource>(
                OperationResult.NotFound, null,
                resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<PrivateEndpointResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        var isUpdate = existing != null;

        var properties = isUpdate ? existing!.Properties : new PrivateEndpointResourceProperties();
        properties.Subnet = request.Properties?.Subnet ?? properties.Subnet;
        properties.PrivateLinkServiceConnections = request.Properties?.PrivateLinkServiceConnections ?? properties.PrivateLinkServiceConnections;
        properties.CustomDnsConfigs = request.Properties?.CustomDnsConfigs ?? properties.CustomDnsConfigs;
        properties.IpConfigurations = request.Properties?.IpConfigurations ?? properties.IpConfigurations;
        properties.NetworkInterfaces = request.Properties?.NetworkInterfaces ?? properties.NetworkInterfaces;
        properties.CustomNetworkInterfaceName =  request.Properties?.CustomNetworkInterfaceName ?? properties.CustomNetworkInterfaceName;
        properties.ManualPrivateLinkServiceConnections =  request.Properties?.ManualPrivateLinkServiceConnections ?? properties.ManualPrivateLinkServiceConnections;

        var resource = new PrivateEndpointResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            name,
            request.Location ?? existing?.Location ?? resourceGroupOperation.Resource!.Location!,
            request.Tags ?? existing?.Tags,
            properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);

        foreach (var ipConfig in request.Properties?.IpConfigurations!)
        {
            logger.LogDebug(nameof(PrivateEndpointControlPlane), nameof(CreateOrUpdate), "Attempting to allocate IP address for Private Endpoint {0}", name);
            
            if (_ipAllocationRegistry.IsAllocated(subscriptionIdentifier, resourceGroupIdentifier, resource.Properties.Subnet!.GetVirtualNetworkName(),
                    ipConfig.Properties?.PrivateIPAddress!))
            {
                return new ControlPlaneOperationResult<PrivateEndpointResource>(OperationResult.Failed, null,
                    $"IP address {ipConfig.Properties?.PrivateIPAddress} is already allocated.", "IpAllocationFailed");
            }
            
            logger.LogDebug(nameof(PrivateEndpointControlPlane), nameof(CreateOrUpdate), "Attempting to allocate IP address {0} for private endpoint.", ipConfig.Properties?.PrivateIPAddress);
            _ipAllocationRegistry.Register(properties.Subnet?.Id!, ipConfig.Properties?.PrivateIPAddress!, resource.Id);
        }

        var operationResult = isUpdate ? OperationResult.Updated : OperationResult.Created;
        return new ControlPlaneOperationResult<PrivateEndpointResource>(operationResult, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<PrivateEndpointResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(NotFoundMessageTemplate, name),
                NotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, name);

        foreach (var ipConfig in resource.Properties?.IpConfigurations!)
        {
            _ipAllocationRegistry.Unregister(resource.Properties.Subnet!.Id, ipConfig.Properties?.PrivateIPAddress!);
        }
        
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<PrivateEndpointResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<PrivateEndpointResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        var filtered = resources.Where(r =>
            r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier));
        return new ControlPlaneOperationResult<PrivateEndpointResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<PrivateEndpointResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<PrivateEndpointResource>(subscriptionIdentifier, null, null, 8);
        var filtered = resources.Where(r => r.IsInSubscription(subscriptionIdentifier));
        return new ControlPlaneOperationResult<PrivateEndpointResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }
}
