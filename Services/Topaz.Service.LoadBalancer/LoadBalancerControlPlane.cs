using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.LoadBalancer.Models;
using Topaz.Service.LoadBalancer.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.LoadBalancer;

internal sealed class LoadBalancerControlPlane(
    Pipeline eventPipeline,
    LoadBalancerResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string LoadBalancerNotFoundCode = "LoadBalancerNotFound";
    private const string LoadBalancerNotFoundMessageTemplate =
        "Load Balancer '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static LoadBalancerControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new LoadBalancerResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var lb = resource.As<LoadBalancerResource, LoadBalancerResourceProperties>();
        if (lb == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Load Balancer instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdate(lb.GetSubscription(), lb.GetResourceGroup(), lb.Name,
            new CreateOrUpdateLoadBalancerRequest
            {
                Location = lb.Location,
                Tags = lb.Tags,
                Sku = lb.Sku,
                Properties = lb.Properties == null ? null : new CreateOrUpdateLoadBalancerRequest.CreateOrUpdateLoadBalancerRequestProperties
                {
                    FrontendIPConfigurations = lb.Properties.FrontendIPConfigurations,
                    BackendAddressPools = lb.Properties.BackendAddressPools,
                    LoadBalancingRules = lb.Properties.LoadBalancingRules,
                    Probes = lb.Properties.Probes,
                    InboundNatRules = lb.Properties.InboundNatRules,
                    OutboundRules = lb.Properties.OutboundRules
                }
            });

        return result.Result;
    }

    public ControlPlaneOperationResult<LoadBalancerResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string loadBalancerName)
    {
        var resource =
            provider.GetAs<LoadBalancerResource>(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.NotFound, null,
                string.Format(LoadBalancerNotFoundMessageTemplate, loadBalancerName), LoadBalancerNotFoundCode);
        }

        return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<LoadBalancerResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string loadBalancerName, CreateOrUpdateLoadBalancerRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.NotFound, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var properties = LoadBalancerResourceProperties.FromRequest(request);
        var resource = new LoadBalancerResource(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName,
            resourceGroupOperation.Resource!.Location!, properties, request.Sku);

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName, resource);

        return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<LoadBalancerResource> Delete(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string loadBalancerName)
    {
        var resource = provider.GetAs<LoadBalancerResource>(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.NotFound, null,
                string.Format(LoadBalancerNotFoundMessageTemplate, loadBalancerName), LoadBalancerNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName);
        return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.Success, null, null, null);
    }

    public ControlPlaneOperationResult<LoadBalancerResource> UpdateTags(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string loadBalancerName, UpdateLoadBalancerTagsRequest request)
    {
        var resource = provider.GetAs<LoadBalancerResource>(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.NotFound, null,
                string.Format(LoadBalancerNotFoundMessageTemplate, loadBalancerName), LoadBalancerNotFoundCode);
        }

        resource.Tags = request.Tags ?? new Dictionary<string, string>();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName, resource);

        return new ControlPlaneOperationResult<LoadBalancerResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<LoadBalancerResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<LoadBalancerResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<LoadBalancerResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<LoadBalancerResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<LoadBalancerResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<LoadBalancerResource[]>(OperationResult.Success, resources, null, null);
    }
}
