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
    private const string EventHubNamespaceNotFoundCode = "EventHubNamespaceNotFound";
    private const string EventHubNotFoundCode = "EventHubNotFound";
    private const string EventHubNamespaceNotFoundMessageTemplate = "Event hub namespace '{0}' could not be found";
    private const string EventHubNotFoundMessageTemplate =
        "Event hub '{0}' could not be found";
    
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

            return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Created, resource, null, null);
        }

        properties.UpdatedAt = DateTime.UtcNow;
        existingNamespace.UpdateProperties(properties);
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, existingNamespace);

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
        
        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, properties);
        
        return new ControlPlaneOperationResult<EventHubResource>(OperationResult.Updated, existingHub, null, null);
    }

    public ControlPlaneOperationResult<EventHubNamespaceResource> Delete(string name, EventHubNamespaceIdentifier namespaceName)
    {
        logger.LogDebug($"Executing {nameof(Delete)}: {name} {namespaceName}");

        if (!provider.EventHubExists(namespaceName.Value, name))
        {
            return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.NotFound, null, string.Format(EventHubNotFoundMessageTemplate, name),
                EventHubNotFoundMessageTemplate);
        }

        provider.DeleteEventHub(name, namespaceName.Value);
        return new ControlPlaneOperationResult<EventHubNamespaceResource>(OperationResult.Deleted, null, null, null);
    }
    
    public ControlPlaneOperationResult<EventHubNamespaceResource> DeleteNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceName)
    {
        logger.LogDebug($"Executing {nameof(DeleteNamespace)}: {namespaceName}");
        
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

        var result = CreateOrUpdateNamespace(@namespace.GetSubscription(), @namespace.GetResourceGroup(), @namespace.Location,
            EventHubNamespaceIdentifier.From(@namespace.Name),
            new CreateOrUpdateEventHubNamespaceRequest());

        return result.Result;
    }
}