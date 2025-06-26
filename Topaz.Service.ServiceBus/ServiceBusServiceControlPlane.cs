using Topaz.Service.ServiceBus.Domain;
using Topaz.Service.ServiceBus.Models;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

internal sealed class ServiceBusServiceControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public (OperationResult result, ServiceBusNamespaceResource? resource) CreateOrUpdateNamespace(SubscriptionIdentifier subscription, ResourceGroupIdentifier resourceGroup, string location,
        ServiceBusNamespaceIdentifier @namespace, CreateOrUpdateServiceBusNamespaceRequest request)
    {
        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(@namespace.Value);
        var properties = ServiceBusNamespaceResourceProperties.From(request);
        
        if (existingNamespace == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;

            var resource = new ServiceBusNamespaceResource(subscription, resourceGroup, location, @namespace, properties);
            provider.CreateOrUpdate(@namespace.Value, resource);
            
            return (OperationResult.Created, resource);
        }
        
        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(@namespace.Value, properties);

        return (OperationResult.Updated, existingNamespace);
    }

    public OperationResult DeleteNamespace(string namespaceName)
    {
        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(namespaceName);
        if (existingNamespace == null)
        {
            return OperationResult.NotFound;
        }
        
        provider.Delete(namespaceName);
        return OperationResult.Deleted;
    }

    public (OperationResult result, ServiceBusNamespaceResource? resource) GetNamespace(
        ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(namespaceIdentifier.Value);
        return existingNamespace == null ? (OperationResult.NotFound, null) : (OperationResult.Success, existingNamespace);
    }

    public (OperationResult result, ServiceBusQueueResource? resource) CreateOrUpdateQueue(
        SubscriptionIdentifier subscription, ResourceGroupIdentifier resourceGroup,
        ServiceBusNamespaceIdentifier @namespace, string queueName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        if (existingQueue == null)
        {
            var properties = new ServiceBusQueueResourceProperties();
            var resource = new ServiceBusQueueResource(subscription, resourceGroup, @namespace, queueName, properties);
            provider.CreateOrUpdateSubresource(queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant(), resource);
            
            return (OperationResult.Created, resource);
        }
        
        return (OperationResult.Updated, existingQueue);
    }

    public OperationResult DeleteQueue(ServiceBusNamespaceIdentifier @namespace, string queueName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        if (existingQueue == null)
        {
            return OperationResult.NotFound;
        }
        
        provider.DeleteSubresource(queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        return OperationResult.Deleted;
    }
}