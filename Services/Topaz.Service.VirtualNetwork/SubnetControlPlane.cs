using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class SubnetControlPlane(
    VirtualNetworkControlPlane vnetControlPlane,
    VirtualNetworkResourceProvider provider,
    ITopazLogger logger)
{
    private const string SubresourceName = "subnets";

    private const string SubnetNotFoundCode = "SubnetNotFound";
    private const string SubnetNotFoundMessageTemplate = "Subnet '{0}' could not be found in virtual network '{1}'";
    private const string VirtualNetworkNotFoundCode = "VirtualNetworkNotFound";
    private const string VirtualNetworkNotFoundMessageTemplate = "Virtual network '{0}' could not be found";

    public static SubnetControlPlane New(Pipeline eventPipeline, ITopazLogger logger)
    {
        var provider = new VirtualNetworkResourceProvider(logger);
        var vnetControlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);
        return new SubnetControlPlane(vnetControlPlane, provider, logger);
    }

    public ControlPlaneOperationResult<SubnetResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName,
        string subnetName,
        CreateOrUpdateSubnetRequest request)
    {
        logger.LogDebug(nameof(SubnetControlPlane), nameof(CreateOrUpdate),
            "Executing {0}: {1} / {2}", nameof(CreateOrUpdate), virtualNetworkName, subnetName);

        var vnetOperation = vnetControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (vnetOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<SubnetResource>(
                OperationResult.NotFound, null,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName),
                VirtualNetworkNotFoundCode);
        }

        var existing = provider.GetSubresourceAs<SubnetResource>(
            subscriptionIdentifier, resourceGroupIdentifier, subnetName, virtualNetworkName, SubresourceName);

        var properties = SubnetResourceProperties.FromRequest(request);
        var resource = new SubnetResource(
            subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, subnetName, properties);

        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, subnetName, virtualNetworkName, SubresourceName, resource);

        var result = existing == null ? OperationResult.Created : OperationResult.Updated;
        return new ControlPlaneOperationResult<SubnetResource>(result, resource, null, null);
    }

    public ControlPlaneOperationResult<SubnetResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName,
        string subnetName)
    {
        logger.LogDebug(nameof(SubnetControlPlane), nameof(Get),
            "Executing {0}: {1} / {2}", nameof(Get), virtualNetworkName, subnetName);

        var vnetOperation = vnetControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (vnetOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<SubnetResource>(
                OperationResult.NotFound, null,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName),
                VirtualNetworkNotFoundCode);
        }

        var resource = provider.GetSubresourceAs<SubnetResource>(
            subscriptionIdentifier, resourceGroupIdentifier, subnetName, virtualNetworkName, SubresourceName);

        return resource == null
            ? new ControlPlaneOperationResult<SubnetResource>(
                OperationResult.NotFound, null,
                string.Format(SubnetNotFoundMessageTemplate, subnetName, virtualNetworkName),
                SubnetNotFoundCode)
            : new ControlPlaneOperationResult<SubnetResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName,
        string subnetName)
    {
        logger.LogDebug(nameof(SubnetControlPlane), nameof(Delete),
            "Executing {0}: {1} / {2}", nameof(Delete), virtualNetworkName, subnetName);

        var vnetOperation = vnetControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (vnetOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName),
                VirtualNetworkNotFoundCode);
        }

        var existing = provider.GetSubresourceAs<SubnetResource>(
            subscriptionIdentifier, resourceGroupIdentifier, subnetName, virtualNetworkName, SubresourceName);

        if (existing == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(SubnetNotFoundMessageTemplate, subnetName, virtualNetworkName),
                SubnetNotFoundCode);
        }

        provider.DeleteSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, subnetName, virtualNetworkName, SubresourceName);

        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<SubnetResource[]> List(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName)
    {
        logger.LogDebug(nameof(SubnetControlPlane), nameof(List),
            "Executing {0}: {1}", nameof(List), virtualNetworkName);

        var vnetOperation = vnetControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (vnetOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<SubnetResource[]>(
                OperationResult.NotFound, null,
                string.Format(VirtualNetworkNotFoundMessageTemplate, virtualNetworkName),
                VirtualNetworkNotFoundCode);
        }

        var resources = provider.ListSubresourcesAs<SubnetResource>(
            subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, SubresourceName);

        return new ControlPlaneOperationResult<SubnetResource[]>(OperationResult.Success, resources, null, null);
    }
}
