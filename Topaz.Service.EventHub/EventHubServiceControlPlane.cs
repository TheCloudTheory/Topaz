using System.Text.Json;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.EventHub.Models;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubServiceControlPlane(ResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    public (OperationResult result, EventHubNamespaceResource? resource) CreateOrUpdateNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location,
        EventHubNamespaceIdentifier @namespace, CreateOrUpdateEventHubNamespaceRequest request)
    {
        var existingNamespace = provider.GetAs<EventHubNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value);
        var properties = EventHubNamespaceResourceProperties.From(request);

        if (existingNamespace == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;

            var resource = new EventHubNamespaceResource(subscriptionIdentifier, resourceGroupIdentifier, location, @namespace, properties);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, resource);

            return (OperationResult.Created, resource);
        }

        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, properties);

        return (OperationResult.Updated, existingNamespace);
    }

    public (OperationResult result, EventHubNamespaceResource? resource) GetNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier)
    {
        var existingNamespace = provider.GetAs<EventHubNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value);
        return existingNamespace == null ? (OperationResult.NotFound, null) : (OperationResult.Success, existingNamespace);
    }

    public (OperationResult result, EventHubResource? resource) CreateOrUpdateEventHub(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
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
            
            return (OperationResult.Created, resource);
        }
        
        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, properties);
        
        return (OperationResult.Updated, existingHub);
    }

    public void Delete(string name, string namespaceName)
    {
        logger.LogDebug($"Executing {nameof(Delete)}: {name} {namespaceName}");

        if (!provider.EventHubExists(namespaceName, name))
        {
            // TODO: Return proper error
            return;
        }

        provider.DeleteEventHub(name, namespaceName);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        return resource.Type == "Microsoft.EventHub/namespaces" ? DeployEventHubNamespace(resource) : DeployEventHub(resource);
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

        return result.result;
    }

    private OperationResult DeployEventHubNamespace(GenericResource resource)
    {
        var @namespace = resource.As<EventHubNamespaceResource, EventHubNamespaceResourceProperties>();
        if (@namespace == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Event Hub namespace instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdateNamespace(@namespace.GetSubscription(), @namespace.GetResourceGroup(), @namespace.Location,
            EventHubNamespaceIdentifier.From(@namespace.Name),
            new CreateOrUpdateEventHubNamespaceRequest());

        return result.result;
    }
}