using System.Net;
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

internal sealed class VirtualNetworkControlPlane(
    Pipeline eventPipeline,
    VirtualNetworkResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string VirtualNetworkNotFoundCode = "VirtualNetworkNotFound";

    private const string VirtualNetworkNotFoundMessageTemplate =
        "Virtual network '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static VirtualNetworkControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new VirtualNetworkResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var vnet = resource.As<VirtualNetworkResource, VirtualNetworkResourceProperties>();
        if (vnet == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Virtual Network instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdate(vnet.GetSubscription(), vnet.GetResourceGroup(), vnet.Name,
            new CreateOrUpdateVirtualNetworkRequest
            {
                Properties = new CreateOrUpdateVirtualNetworkRequest.CreateOrUpdateVirtualNetworkRequestProperties
                {
                    AddressSpace = vnet.Properties.AddressSpace,
                    Subnets = vnet.Properties.Subnets.HasValue
                        ? JsonSerializer.Deserialize<IList<CreateOrUpdateVirtualNetworkRequest.InlineSubnetEntry>>(
                            vnet.Properties.Subnets.Value.GetRawText(), GlobalSettings.JsonOptions)
                        : null,
                    EnableDdosProtection = vnet.Properties.EnableDdosProtection,
                    Encryption = vnet.Properties.Encryption,
                    BgpCommunities = vnet.Properties.BgpCommunities,
                    FlowLogs = vnet.Properties.FlowLogs,
                    FlowTimeoutInMinutes = vnet.Properties.FlowTimeoutInMinutes,
                    EnableVmProtection = vnet.Properties.EnableVmProtection,
                    PrivateEndpointVnetPolicy = vnet.Properties.PrivateEndpointVnetPolicy
                }
            });

        return result.Result;
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
        var resource = new VirtualNetworkResource(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName,
            resourceGroupOperation.Resource!.Location!, properties);

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, resource);

        if (request.Properties?.Subnets != null)
        {
            var subnetControlPlane = new SubnetControlPlane(this, provider, logger);
            foreach (var inlineSubnet in request.Properties.Subnets)
            {
                if (string.IsNullOrWhiteSpace(inlineSubnet.Name)) continue;
                subnetControlPlane.CreateOrUpdate(
                    subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName,
                    inlineSubnet.Name,
                    new CreateOrUpdateSubnetRequest { Properties = inlineSubnet.Properties });
            }
        }

        return new ControlPlaneOperationResult<VirtualNetworkResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<IpAddressAvailabilityResult> CheckIpAddressAvailability(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName,
        string ipAddress)
    {
        var vnetOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (vnetOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<IpAddressAvailabilityResult>(
                OperationResult.NotFound, null,
                vnetOperation.Reason, vnetOperation.Code);
        }

        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            return new ControlPlaneOperationResult<IpAddressAvailabilityResult>(
                OperationResult.Success,
                new IpAddressAvailabilityResult { Available = false },
                null, null);
        }

        var subnetsControlPlane = new SubnetControlPlane(this, new VirtualNetworkResourceProvider(logger), logger);
        var subnetsOperation = subnetsControlPlane.List(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        var subnets = subnetsOperation.Resource ?? [];

        var available = subnets.Any(subnet =>
        {
            var prefixes = new List<string?>();
            if (subnet.Properties?.AddressPrefix != null)
                prefixes.Add(subnet.Properties.AddressPrefix);
            if (subnet.Properties?.AddressPrefixes != null)
                prefixes.AddRange(subnet.Properties.AddressPrefixes);

            return prefixes.Any(prefix =>
            {
                if (string.IsNullOrWhiteSpace(prefix)) return false;
                try { return IPNetwork.Parse(prefix).Contains(ip); }
                catch { return false; }
            });
        });

        return new ControlPlaneOperationResult<IpAddressAvailabilityResult>(
            OperationResult.Success,
            new IpAddressAvailabilityResult { Available = available },
            null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName)
    {
        var resource = provider.GetAs<VirtualNetworkResource>(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName),
                VirtualNetworkNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<VirtualNetworkResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<VirtualNetworkResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        var filtered = resources.Where(r =>
            r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier));
        return new ControlPlaneOperationResult<VirtualNetworkResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<VirtualNetworkResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<VirtualNetworkResource>(subscriptionIdentifier, null, null, 8);
        var filtered = resources.Where(r => r.IsInSubscription(subscriptionIdentifier));
        return new ControlPlaneOperationResult<VirtualNetworkResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<VirtualNetworkResource> UpdateTags(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName,
        UpdateVirtualNetworkTagsRequest request)
    {
        var resource = provider.GetAs<VirtualNetworkResource>(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<VirtualNetworkResource>(
                OperationResult.NotFound, null,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName),
                VirtualNetworkNotFoundCode);
        }

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, resource);
        return new ControlPlaneOperationResult<VirtualNetworkResource>(OperationResult.Updated, resource, null, null);
    }
}