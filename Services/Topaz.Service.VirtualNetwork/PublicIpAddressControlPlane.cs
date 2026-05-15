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

internal sealed class PublicIpAddressControlPlane(
    Pipeline eventPipeline,
    PublicIpAddressResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string PipNotFoundCode = "PublicIPAddressNotFound";

    private const string PipNotFoundMessageTemplate =
        "Public IP address '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static PublicIpAddressControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new PublicIpAddressResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var pip = resource.As<PublicIpAddressResource, PublicIpAddressResourceProperties>();
        if (pip == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Public IP Address instance.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(pip.GetSubscription(), pip.GetResourceGroup(), pip.Name,
                new CreateOrUpdatePublicIpAddressRequest
                {
                    Location = pip.Location,
                    Tags = pip.Tags,
                    Properties = new CreateOrUpdatePublicIpAddressRequest.CreateOrUpdatePublicIpAddressRequestProperties
                    {
                        PublicIPAllocationMethod = pip.Properties.PublicIPAllocationMethod,
                        PublicIPAddressVersion = pip.Properties.PublicIPAddressVersion,
                        IdleTimeoutInMinutes = pip.Properties.IdleTimeoutInMinutes
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

    public ControlPlaneOperationResult<PublicIpAddressResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<PublicIpAddressResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<PublicIpAddressResource>(
                OperationResult.NotFound, null,
                string.Format(PipNotFoundMessageTemplate, name), PipNotFoundCode);
        }

        return new ControlPlaneOperationResult<PublicIpAddressResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<PublicIpAddressResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        CreateOrUpdatePublicIpAddressRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PublicIpAddressResource>(
                OperationResult.NotFound, null,
                resourceGroupOperation.Reason, resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<PublicIpAddressResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        var isUpdate = existing != null;

        PublicIpAddressResourceProperties properties;
        if (isUpdate)
        {
            properties = existing!.Properties;
            PublicIpAddressResourceProperties.UpdateFromRequest(properties, request);
        }
        else
        {
            properties = PublicIpAddressResourceProperties.FromRequest(request);
        }

        var resource = new PublicIpAddressResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            name,
            request.Location ?? resourceGroupOperation.Resource!.Location!,
            request.Tags,
            properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);

        var operationResult = isUpdate ? OperationResult.Updated : OperationResult.Created;
        return new ControlPlaneOperationResult<PublicIpAddressResource>(operationResult, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name)
    {
        var resource = provider.GetAs<PublicIpAddressResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(PipNotFoundMessageTemplate, name),
                PipNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<PublicIpAddressResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<PublicIpAddressResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        var filtered = resources.Where(r =>
            r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier));
        return new ControlPlaneOperationResult<PublicIpAddressResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<PublicIpAddressResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<PublicIpAddressResource>(subscriptionIdentifier, null, null, 8);
        var filtered = resources.Where(r => r.IsInSubscription(subscriptionIdentifier));
        return new ControlPlaneOperationResult<PublicIpAddressResource[]>(OperationResult.Success, filtered.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<PublicIpAddressResource> UpdateTags(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        UpdatePublicIpAddressTagsRequest request)
    {
        var resource = provider.GetAs<PublicIpAddressResource>(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<PublicIpAddressResource>(
                OperationResult.NotFound, null,
                string.Format(PipNotFoundMessageTemplate, name),
                PipNotFoundCode);
        }

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, name, resource);
        return new ControlPlaneOperationResult<PublicIpAddressResource>(OperationResult.Updated, resource, null, null);
    }
}
