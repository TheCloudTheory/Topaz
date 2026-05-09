using System.Text.Json;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.EventHub.Models;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubServiceControlPlane(EventHubResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    private const string EventHubNamespaceNotFoundCode = "EventHubNamespaceNotFound";
    private const string EventHubNotFoundCode = "EventHubNotFound";
    private const string EventHubNamespaceNotFoundMessageTemplate = "Event hub namespace '{0}' could not be found";
    private const string EventHubNotFoundMessageTemplate =
        "Event hub '{0}' could not be found";
    
    public static EventHubServiceControlPlane New(ITopazLogger logger) => new(new EventHubResourceProvider(logger), logger);
    
    public ControlPlaneOperationResult<EventHubNamespaceResource> CreateOrUpdateNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location,
        EventHubNamespaceIdentifier @namespace, CreateOrUpdateEventHubNamespaceRequest request)
    {
        var existingNamespace = provider.GetAs<EventHubNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value);
        var properties = EventHubNamespaceResourceProperties.From(request);

        if (existingNamespace == null)
        {
            properties.CreatedAt = DateTime.UtcNow;
            properties.UpdatedAt = DateTime.UtcNow;
            properties.ProvisioningState = "Succeeded";

            var resource = new EventHubNamespaceResource(subscriptionIdentifier, resourceGroupIdentifier, location, @namespace, properties);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, resource, true);
            CreateOrUpdateNetworkRuleSet(subscriptionIdentifier, resourceGroupIdentifier, @namespace, "default",
                EventHubNetworkRuleSetSubresourceProperties.Default());

            return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Created, resource, null, null);
        }

        properties.UpdatedAt = DateTime.UtcNow;
        existingNamespace.UpdateProperties(properties);
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, existingNamespace);
        EnsureDefaultNetworkRuleSet(subscriptionIdentifier, resourceGroupIdentifier, @namespace);

        return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Updated, existingNamespace, null, null);
    }

    public ControlPlaneOperationResult<EventHubNamespaceResource> GetNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier)
    {
        var existingNamespace = provider.GetAs<EventHubNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value);
        return existingNamespace == null
            ? new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.NotFound, null,
                string.Format(EventHubNamespaceNotFoundMessageTemplate, namespaceIdentifier),
                EventHubNamespaceNotFoundCode)
            : new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Success, existingNamespace,
                null, null);
    }

    public ControlPlaneOperationResult<EventHubResource> GetEventHub(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier, string hubName)
    {
        var existingHub = provider.GetSubresourceAs<EventHubResource>(subscriptionIdentifier, resourceGroupIdentifier,
            hubName, namespaceIdentifier.Value, nameof(Subresource.Hubs).ToLowerInvariant());

        return existingHub == null
            ? new ControlPlaneOperationResult<EventHubResource>(OperationResult.NotFound, null,
                string.Format(EventHubNotFoundMessageTemplate, hubName), EventHubNotFoundCode)
            : new ControlPlaneOperationResult<EventHubResource>(OperationResult.Success, existingHub, null, null);
    }

    public ControlPlaneOperationResult<EventHubResource[]> ListEventHubs(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier)
    {
        var existingNamespace = GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (existingNamespace.Result == OperationResult.NotFound || existingNamespace.Resource == null)
        {
            return new ControlPlaneOperationResult<EventHubResource[]>(OperationResult.NotFound, null,
                string.Format(EventHubNamespaceNotFoundMessageTemplate, namespaceIdentifier),
                EventHubNamespaceNotFoundCode);
        }

        var hubs = provider.ListSubresourcesAs<EventHubResource>(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier.Value, nameof(Subresource.Hubs).ToLowerInvariant());

        logger.LogDebug(nameof(EventHubServiceControlPlane), nameof(ListEventHubs),
            "Found {0} event hubs in namespace '{1}'.", hubs.Length, namespaceIdentifier);

        return new ControlPlaneOperationResult<EventHubResource[]>(OperationResult.Success, hubs, null, null);
    }

    public ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource> GetNetworkRuleSet(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier, string networkRuleSetName)
    {
        var existingNamespace = GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (existingNamespace.Result == OperationResult.NotFound || existingNamespace.Resource == null)
        {
            return new ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource>(OperationResult.NotFound, null,
                string.Format(EventHubNamespaceNotFoundMessageTemplate, namespaceIdentifier),
                EventHubNamespaceNotFoundCode);
        }

        var networkRuleSet = provider.GetSubresourceAs<EventHubNetworkRuleSetSubresource>(subscriptionIdentifier,
            resourceGroupIdentifier, networkRuleSetName, namespaceIdentifier.Value,
            nameof(Subresource.NetworkRuleSets).ToLowerInvariant());

        if (networkRuleSet != null)
        {
            return new ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource>(OperationResult.Success,
                networkRuleSet, null, null);
        }

        var created = CreateOrUpdateNetworkRuleSet(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier,
            networkRuleSetName, EventHubNetworkRuleSetSubresourceProperties.Default());
        return new ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource>(created.Result, created.Resource,
            null, null);
    }

    public ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource> CreateOrUpdateNetworkRuleSet(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier, string networkRuleSetName,
        EventHubNetworkRuleSetSubresourceProperties properties)
    {
        var existingNetworkRuleSet = provider.GetSubresourceAs<EventHubNetworkRuleSetSubresource>(
            subscriptionIdentifier, resourceGroupIdentifier, networkRuleSetName, namespaceIdentifier.Value,
            nameof(Subresource.NetworkRuleSets).ToLowerInvariant());

        if (existingNetworkRuleSet == null)
        {
            var resource = new EventHubNetworkRuleSetSubresource(subscriptionIdentifier, resourceGroupIdentifier,
                namespaceIdentifier, networkRuleSetName, properties);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, networkRuleSetName,
                namespaceIdentifier.Value, nameof(Subresource.NetworkRuleSets).ToLowerInvariant(), resource);

            return new ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource>(OperationResult.Created,
                resource, null, null);
        }

        var updated = new EventHubNetworkRuleSetSubresource(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier, networkRuleSetName, properties);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, networkRuleSetName,
            namespaceIdentifier.Value, nameof(Subresource.NetworkRuleSets).ToLowerInvariant(), updated);

        return new ControlPlaneOperationResult<EventHubNetworkRuleSetSubresource>(OperationResult.Updated, updated,
            null, null);
    }

    public ControlPlaneOperationResult<EventHubResource> CreateOrUpdateEventHub(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier @namespace, string hubName, CreateOrUpdateEventHubRequest request)
    {
        var existingHub = provider.GetSubresourceAs<EventHubResource>(subscriptionIdentifier, resourceGroupIdentifier,
            hubName, @namespace.Value, nameof(Subresource.Hubs).ToLowerInvariant());
        var properties = EventHubResourceProperties.From(request);
        if (existingHub == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;
            
            var resource = new EventHubResource(subscriptionIdentifier, resourceGroupIdentifier, @namespace, hubName, properties);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, hubName,
                @namespace.Value, nameof(Subresource.Hubs).ToLowerInvariant(), resource);
            
            return new ControlPlaneOperationResult<EventHubResource>(OperationResult.Created, resource, null, null);
        }
        
        existingHub.UpdateProperties(properties);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, hubName,
            @namespace.Value, nameof(Subresource.Hubs).ToLowerInvariant(), existingHub);

        return new ControlPlaneOperationResult<EventHubResource>(OperationResult.Updated, existingHub, null, null);
    }

    public ControlPlaneOperationResult<EventHubNamespaceResource> Delete(string name, EventHubNamespaceIdentifier namespaceName)
    {
        logger.LogDebug(nameof(EventHubServiceControlPlane), nameof(Delete), "Executing {0}: {1} {2}", nameof(Delete), name, namespaceName);

        if (!provider.EventHubExists(namespaceName.Value, name))
        {
            return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.NotFound, null, string.Format(EventHubNotFoundMessageTemplate, name),
                EventHubNotFoundCode);
        }

        provider.DeleteEventHub(name, namespaceName.Value);
        return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Deleted, null, null, null);
    }
    
    public ControlPlaneOperationResult<EventHubNamespaceResource> DeleteNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceName)
    {
        logger.LogDebug(nameof(EventHubServiceControlPlane), nameof(DeleteNamespace), "Executing {0}: {1}", nameof(DeleteNamespace), namespaceName);
        
        var existingNamespace = GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        if (existingNamespace.Resource == null || existingNamespace.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.NotFound, null, string.Format(EventHubNamespaceNotFoundMessageTemplate, namespaceName),
                EventHubNamespaceNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, namespaceName.Value);
        return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Deleted, null, null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        return resource.Type switch
        {
            "Microsoft.EventHub/namespaces" => DeployEventHubNamespace(resource),
            "Microsoft.EventHub/namespaces/networkRuleSets" => DeployEventHubNetworkRuleSet(resource),
            _ => DeployEventHub(resource)
        };
    }

    public ControlPlaneOperationResult<EventHubNamespaceResource[]> ListNamespacesBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider
            .ListAs<EventHubNamespaceResource>(subscriptionIdentifier, null, null, 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        logger.LogDebug(nameof(EventHubServiceControlPlane), nameof(ListNamespacesBySubscription),
            "Found {0} namespaces in subscription {1}.", resources.Length, subscriptionIdentifier);

        return new ControlPlaneOperationResult<EventHubNamespaceResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<EventHubNamespaceResource[]> ListNamespaces(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider
            .ListAs<EventHubNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        logger.LogDebug(nameof(EventHubServiceControlPlane), nameof(ListNamespaces),
            "Found {0} namespaces.", resources.Length);

        return new ControlPlaneOperationResult<EventHubNamespaceResource[]>(OperationResult.Success, resources, null, null);
    }

    private OperationResult DeployEventHub(GenericResource resource)
    {
        var hub = resource.AsSubresource<EventHubResource, EventHubResourceProperties>();
        if (hub == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Event Hub instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdateEventHub(hub.GetSubscription(), hub.GetResourceGroup(),
            hub.GetNamespace(),
            hub.Name,
            new CreateOrUpdateEventHubRequest());

        return result.Result;
    }

    private OperationResult DeployEventHubNamespace(GenericResource resource)
    {
        var @namespace = resource.As<EventHubNamespaceResource, EventHubNamespaceResourceProperties>();
        if (@namespace == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Event Hub namespace instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdateNamespace(@namespace.GetSubscription(), @namespace.GetResourceGroup(), @namespace.Location!,
            EventHubNamespaceIdentifier.From(@namespace.Name),
            new CreateOrUpdateEventHubNamespaceRequest());

        return result.Result;
    }

    private void EnsureDefaultNetworkRuleSet(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, EventHubNamespaceIdentifier namespaceIdentifier)
    {
        var existing = provider.GetSubresourceAs<EventHubNetworkRuleSetSubresource>(subscriptionIdentifier,
            resourceGroupIdentifier, "default", namespaceIdentifier.Value,
            nameof(Subresource.NetworkRuleSets).ToLowerInvariant());

        if (existing != null)
        {
            return;
        }

        CreateOrUpdateNetworkRuleSet(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, "default",
            EventHubNetworkRuleSetSubresourceProperties.Default());
    }

        private OperationResult DeployEventHubNetworkRuleSet(GenericResource resource)
    {
        var networkRuleSet =
            resource.AsSubresource<EventHubNetworkRuleSetSubresource, EventHubNetworkRuleSetSubresourceProperties>();
        if (networkRuleSet == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as an Event Hub network ruleset.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdateNetworkRuleSet(networkRuleSet.GetSubscription(), networkRuleSet.GetResourceGroup(),
            networkRuleSet.GetNamespace(), networkRuleSet.Name, networkRuleSet.Properties);

        return result.Result;
    }
}