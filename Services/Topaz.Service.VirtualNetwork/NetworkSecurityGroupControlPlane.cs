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

internal sealed class NetworkSecurityGroupControlPlane(
    Pipeline eventPipeline,
    NetworkSecurityGroupResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NsgNotFoundCode = "NetworkSecurityGroupNotFound";

    private const string NsgNotFoundMessageTemplate =
        "Network security group '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static NetworkSecurityGroupControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new NetworkSecurityGroupResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var nsg = resource.As<NetworkSecurityGroupResource, NetworkSecurityGroupResourceProperties>();
        if (nsg == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Network Security Group instance.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(nsg.GetSubscription(), nsg.GetResourceGroup(), nsg.Name,
                new CreateOrUpdateNetworkSecurityGroupRequest
                {
                    Location = nsg.Location,
                    Tags = nsg.Tags,
                    Properties = new CreateOrUpdateNetworkSecurityGroupRequest.CreateOrUpdateNetworkSecurityGroupRequestProperties
                    {
                        SecurityRules = nsg.Properties.SecurityRules
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

    public ControlPlaneOperationResult<NetworkSecurityGroupResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<NetworkSecurityGroupResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<NetworkSecurityGroupResource>(
                OperationResult.NotFound, null,
                string.Format(NsgNotFoundMessageTemplate, name), NsgNotFoundCode);
        }

        return new ControlPlaneOperationResult<NetworkSecurityGroupResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<NetworkSecurityGroupResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        CreateOrUpdateNetworkSecurityGroupRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<NetworkSecurityGroupResource>(
                OperationResult.NotFound, null,
                resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }

        var isUpdate = provider.GetAs<NetworkSecurityGroupResource>(subscriptionIdentifier, resourceGroupIdentifier, name) != null;

        var properties = NetworkSecurityGroupResourceProperties.FromRequest(request);
        var resource = new NetworkSecurityGroupResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            name,
            request.Location ?? resourceGroupOperation.Resource!.Location!,
            request.Tags,
            properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);

        var operationResult = isUpdate ? OperationResult.Updated : OperationResult.Created;
        return new ControlPlaneOperationResult<NetworkSecurityGroupResource>(operationResult, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<NetworkSecurityGroupResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(NsgNotFoundMessageTemplate, name),
                NsgNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<NetworkSecurityGroupResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<NetworkSecurityGroupResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        var filtered = resources.Where(r =>
            r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier));
        return new ControlPlaneOperationResult<NetworkSecurityGroupResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<NetworkSecurityGroupResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<NetworkSecurityGroupResource>(subscriptionIdentifier, null, null, 8);
        var filtered = resources.Where(r => r.IsInSubscription(subscriptionIdentifier));
        return new ControlPlaneOperationResult<NetworkSecurityGroupResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<NetworkSecurityGroupResource> UpdateTags(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        UpdateNetworkSecurityGroupTagsRequest request)
    {
        var resource = provider.GetAs<NetworkSecurityGroupResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<NetworkSecurityGroupResource>(
                OperationResult.NotFound, null,
                string.Format(NsgNotFoundMessageTemplate, name),
                NsgNotFoundCode);
        }

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);
        return new ControlPlaneOperationResult<NetworkSecurityGroupResource>(OperationResult.Updated, resource, null, null);
    }
}
