using System.Text.Json;
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

internal sealed class NetworkInterfaceControlPlane(
    Pipeline eventPipeline,
    NetworkInterfaceResourceProvider provider,
    IpAllocationRegistry ipAllocationRegistry,
    ITopazLogger logger) : IControlPlane
{
    private const string NicNotFoundCode = "NetworkInterfaceNotFound";

    private const string NicNotFoundMessageTemplate =
        "Network interface '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static NetworkInterfaceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new NetworkInterfaceResourceProvider(logger),
            new IpAllocationRegistry(new VirtualNetworkResourceProvider(logger)), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var nic = resource.As<NetworkInterfaceResource, NetworkInterfaceResourceProperties>();
        if (nic == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Network Interface instance.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(nic.GetSubscription(), nic.GetResourceGroup(), nic.Name,
                new CreateOrUpdateNetworkInterfaceRequest
                {
                    Location = nic.Location,
                    Tags = nic.Tags,
                    Properties = new CreateOrUpdateNetworkInterfaceRequest.CreateOrUpdateNetworkInterfaceRequestProperties
                    {
                        IpConfigurations = nic.Properties.IpConfigurations,
                        NetworkSecurityGroup = nic.Properties.NetworkSecurityGroup,
                        EnableAcceleratedNetworking = nic.Properties.EnableAcceleratedNetworking,
                        EnableIpForwarding = nic.Properties.EnableIPForwarding
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

    public ControlPlaneOperationResult<NetworkInterfaceResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<NetworkInterfaceResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<NetworkInterfaceResource>(
                OperationResult.NotFound, null,
                string.Format(NicNotFoundMessageTemplate, name), NicNotFoundCode);
        }

        return new ControlPlaneOperationResult<NetworkInterfaceResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<NetworkInterfaceResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        CreateOrUpdateNetworkInterfaceRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<NetworkInterfaceResource>(
                OperationResult.NotFound, null,
                resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<NetworkInterfaceResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        var isUpdate = existing != null;

        NetworkInterfaceResourceProperties properties;
        if (isUpdate)
        {
            properties = existing!.Properties;
            NetworkInterfaceResourceProperties.UpdateFromRequest(properties, request);
        }
        else
        {
            properties = NetworkInterfaceResourceProperties.FromRequest(request);
        }

        var resource = new NetworkInterfaceResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            name,
            request.Location ?? existing?.Location ?? resourceGroupOperation.Resource!.Location!,
            request.Tags ?? existing?.Tags,
            properties);
        
        var result = ProcessIpAllocations(resource, isUpdate);
        if (!result.success)
        {
            logger.LogError(nameof(NetworkInterfaceControlPlane), nameof(CreateOrUpdate), "Failed to process IP allocation because of {0}", result.error);
            return new ControlPlaneOperationResult<NetworkInterfaceResource>(OperationResult.Conflict, null, result.error,
                "IPAllocationFailed");
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);

        var operationResult = isUpdate ? OperationResult.Updated : OperationResult.Created;
        return new ControlPlaneOperationResult<NetworkInterfaceResource>(operationResult, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<NetworkInterfaceResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(NicNotFoundMessageTemplate, name),
                NicNotFoundCode);
        }

        UnregisterNicIps(resource.Properties);
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<NetworkInterfaceResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<NetworkInterfaceResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        var filtered = resources.Where(r =>
            r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier));
        return new ControlPlaneOperationResult<NetworkInterfaceResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<NetworkInterfaceResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<NetworkInterfaceResource>(subscriptionIdentifier, null, null, 8);
        var filtered = resources.Where(r => r.IsInSubscription(subscriptionIdentifier));
        return new ControlPlaneOperationResult<NetworkInterfaceResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<NetworkInterfaceResource> UpdateTags(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        UpdateNetworkInterfaceTagsRequest request)
    {
        var resource = provider.GetAs<NetworkInterfaceResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<NetworkInterfaceResource>(
                OperationResult.NotFound, null,
                string.Format(NicNotFoundMessageTemplate, name),
                NicNotFoundCode);
        }

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);
        return new ControlPlaneOperationResult<NetworkInterfaceResource>(OperationResult.Updated, resource, null, null);
    }

    private (bool success, string? error) ProcessIpAllocations(
        NetworkInterfaceResource nic,
        bool isUpdate)
    {
        if (isUpdate)
        {
            UnregisterNicIps(nic.Properties);
        }

        var configs = nic.Properties.IpConfigurations;
        var modified = false;

        foreach (var config in configs ?? [])
        {
            var subnetId = config.Properties?.Subnet?.Id;
            if (string.IsNullOrEmpty(subnetId)) continue;

            var isStatic = string.Equals(
                config.Properties?.PrivateIPAllocationMethod, "Static",
                StringComparison.OrdinalIgnoreCase);

            if (isStatic)
            {
                var staticIp = config.Properties?.PrivateIPAddress;
                if (!string.IsNullOrEmpty(staticIp))
                {
                    if (ipAllocationRegistry.IsAllocated(nic.GetSubscription(), nic.GetResourceGroup(),
                            config.Properties?.Subnet?.GetVirtualNetworkName()!, staticIp))
                    {
                        return (false, $"IP address {staticIp} is already allocated.");
                    }
                    
                    ipAllocationRegistry.Register(subnetId, staticIp, nic.Id);
                }
                    
            }
            else
            {
                var assignedIp = ipAllocationRegistry.FindNextAvailableIp(subnetId);
                if (assignedIp == null || config.Properties == null) continue;
                
                config.Properties.PrivateIPAddress = assignedIp;
                config.Properties.PrivateIPAllocationMethod ??= "Dynamic";
                ipAllocationRegistry.Register(subnetId, assignedIp, nic.Id);
                modified = true;
            }
        }

        if (modified)
        {
            nic.Properties.IpConfigurations = configs;
        }
            
        return (true, null);
    }

    private void UnregisterNicIps(NetworkInterfaceResourceProperties properties)
    {
        var configs = properties.IpConfigurations;

        foreach (var config in configs ?? [])
        {
            var subnetId = config.Properties?.Subnet?.Id;
            var ipAddress = config.Properties?.PrivateIPAddress;
            if (!string.IsNullOrEmpty(subnetId) && !string.IsNullOrEmpty(ipAddress))
                ipAllocationRegistry.Unregister(subnetId, ipAddress);
        }
    }
}
