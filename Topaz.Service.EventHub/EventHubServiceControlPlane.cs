using System.Text.Json;
using Azure.Core;
using Topaz.Service.EventHub.Models;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubServiceControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public (OperationResult result, EventHubNamespaceResource? resource) CreateOrUpdateNamespace(
        SubscriptionIdentifier subscription, ResourceGroupIdentifier resourceGroup, AzureLocation location,
        EventHubNamespaceIdentifier @namespace, CreateOrUpdateEventHubNamespaceRequest request)
    {
        var existingNamespace = provider.GetAs<EventHubNamespaceResource>(@namespace.Value);
        var properties = EventHubNamespaceResourceProperties.From(request);

        if (existingNamespace == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;

            var resource = new EventHubNamespaceResource(subscription, resourceGroup, location, @namespace, properties);
            provider.CreateOrUpdate(@namespace.Value, resource);

            return (OperationResult.Created, resource);
        }

        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(@namespace.Value, properties);

        return (OperationResult.Updated, existingNamespace);
    }

    public (OperationResult result, EventHubNamespaceResource? resource) GetNamespace(
        EventHubNamespaceIdentifier namespaceIdentifier)
    {
        var existingNamespace = provider.GetAs<EventHubNamespaceResource>(namespaceIdentifier.Value);
        return existingNamespace == null ? (OperationResult.NotFound, null) : (OperationResult.Success, existingNamespace);
    }

    public (OperationResult result, EventHubResource? resource) CreateOrUpdateEventHub(SubscriptionIdentifier subscription, ResourceGroupIdentifier resourceGroup,
        EventHubNamespaceIdentifier @namespace, string hubName, CreateOrUpdateEventHubRequest request)
    {
        var existingHub = provider.GetSubresourceAs<EventHubResource>(hubName, @namespace.Value, nameof(Subresource.Hubs).ToLowerInvariant());
        var properties = EventHubResourceProperties.From(request);
        if (existingHub == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;
            
            var resource = new EventHubResource(subscription, resourceGroup, @namespace, hubName, properties);
            provider.CreateOrUpdateSubresource(hubName, @namespace.Value, nameof(Subresource.Hubs).ToLowerInvariant(), resource);
            
            return (OperationResult.Created, resource);
        }
        
        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(@namespace.Value, properties);
        
        return (OperationResult.Updated, existingHub);
    }

    public void Delete(string name, string namespaceName)
    {
        logger.LogDebug($"Executing {nameof(Delete)}: {name} {namespaceName}");

        if (provider.EventHubExists(namespaceName, name) == false)
        {
            // TODO: Return proper error
            return;
        }

        provider.DeleteEventHub(name, namespaceName);
    }
}