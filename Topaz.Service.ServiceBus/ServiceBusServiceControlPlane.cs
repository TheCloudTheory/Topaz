using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

internal sealed class ServiceBusServiceControlPlane(ServiceBusResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    private const string ServiceBusQueueNotFoundCode = "ServiceBusQueueNotFound";
    private const string ServiceBusQueueNotFoundMessageTemplate =
        "Service Bus queue '{0}' could not be found";
    private const string ServiceBusTopicNotFoundCode = "ServiceBusTopicNotFound";
    private const string ServiceBusTopicNotFoundMessageTemplate =
        "Service Bus topic '{0}' could not be found";
    
    public static ServiceBusServiceControlPlane New(ITopazLogger logger) => new(new ServiceBusResourceProvider(logger), logger);
    
    public ControlPlaneOperationResult<ServiceBusNamespaceResource> CreateOrUpdateNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        AzureLocation location,
        ServiceBusNamespaceIdentifier @namespace, CreateOrUpdateServiceBusNamespaceRequest request)
    {
        var existingNamespace =
            provider.GetAs<ServiceBusNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier,
                @namespace.Value);
        var properties = ServiceBusNamespaceResourceProperties.From(request);

        if (existingNamespace == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;

            var resource = new ServiceBusNamespaceResource(subscriptionIdentifier, resourceGroupIdentifier, location,
                @namespace, properties);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, resource);

            return new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Created, resource, null, null);
        }

        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, properties);

        return new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Updated, existingNamespace, null, null);
    }

    public OperationResult DeleteNamespace(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName)
    {
        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        if (existingNamespace == null)
        {
            return OperationResult.NotFound;
        }
        
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        return OperationResult.Deleted;
    }

    public (OperationResult result, ServiceBusNamespaceResource? resource) GetNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value);
        return existingNamespace == null ? (OperationResult.NotFound, null) : (OperationResult.Success, existingNamespace);
    }

    public ControlPlaneOperationResult<ServiceBusQueueResource> CreateOrUpdateQueue(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier @namespace, string queueName,  CreateOrUpdateServiceBusQueueRequest request)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        var properties = ServiceBusQueueResourceProperties.From(request);
        if (existingQueue == null)
        {
            properties.CreatedOn = DateTime.UtcNow;
            properties.UpdatedOn = DateTime.UtcNow;
            
            var resource = new ServiceBusQueueResource(subscriptionIdentifier, resourceGroupIdentifier, @namespace, queueName, properties);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, queueName,
                @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant(), resource);
            
            return new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Created, resource, null, null);
        }
        
        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, properties);
        
        return new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Updated, existingQueue, null, null);
    }

    public OperationResult DeleteQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier @namespace, string queueName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier, resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        if (existingQueue == null)
        {
            return OperationResult.NotFound;
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        return OperationResult.Deleted;
    }

    public ControlPlaneOperationResult<ServiceBusQueueResource> GetQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier @namespace, string queueName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        return existingQueue == null
            ? new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.NotFound, null, ServiceBusQueueNotFoundMessageTemplate, ServiceBusQueueNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Success, existingQueue, null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        return resource.Type == "Microsoft.ServiceBus/namespaces" ? DeployServiceBusNamespace(resource) : DeployServiceBusQueue(resource);
    }

    private OperationResult DeployServiceBusQueue(GenericResource resource)
    {
        var queue = resource.AsSubresource<ServiceBusQueueResource, ServiceBusQueueResourceProperties>();
        if (queue == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Service Bus namespace.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdateQueue(queue.GetSubscription(), queue.GetResourceGroup(),
            queue.GetNamespace(),
            queue.Name,
            CreateOrUpdateServiceBusQueueRequest.From(resource));

        return result.Result;
    }

    private OperationResult DeployServiceBusNamespace(GenericResource resource)
    {
        var @namespace = resource.As<ServiceBusNamespaceResource, ServiceBusNamespaceResourceProperties>();
        if (@namespace == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Service Bus namespace.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdateNamespace(@namespace.GetSubscription(), @namespace.GetResourceGroup(),
            @namespace.Location,
            ServiceBusNamespaceIdentifier.From(@namespace.Name),
            CreateOrUpdateServiceBusNamespaceRequest.From(resource));

        return result.Result;
    }

    public ControlPlaneOperationResult<ServiceBusTopicResource> GetTopic(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusTopicResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value, nameof(Subresource.Queues).ToLowerInvariant());
        return existingQueue == null
            ? new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.NotFound, null, ServiceBusTopicNotFoundMessageTemplate, ServiceBusTopicNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Success, existingQueue, null, null);
    }

    public ControlPlaneOperationResult<ServiceBusTopicResource> CreateOrUpdateTopic(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName, CreateOrUpdateServiceBusTopicRequest request)
    {
        var existingTopic = provider.GetSubresourceAs<ServiceBusTopicResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value, nameof(Subresource.Queues).ToLowerInvariant());
        var properties = ServiceBusTopicResourceProperties.From(request);
        if (existingTopic == null)
        {
            var resource = new ServiceBusTopicResource(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, topicName, properties);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, topicName,
                namespaceIdentifier.Value, nameof(Subresource.Queues).ToLowerInvariant(), resource);
            
            return new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Created, resource, null, null);
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value, properties);
        
        return new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Updated, existingTopic, null, null);
    }
}