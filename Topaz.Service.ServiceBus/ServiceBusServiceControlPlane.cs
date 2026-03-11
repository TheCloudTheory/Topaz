using System.Collections.Immutable;
using System.Xml.Linq;
using Azure.Core;
using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

internal sealed class ServiceBusServiceControlPlane(
    ServiceBusResourceProvider provider,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private const string ServiceBusNamespaceNotFoundCode = "ServiceBusNamespaceNotFound";

    private const string ServiceBusNamespaceNotFoundMessageTemplate =
        "Service Bus namespace '{0}' could not be found";

    private const string ServiceBusQueueNotFoundCode = "ServiceBusQueueNotFound";

    private const string ServiceBusQueueNotFoundMessageTemplate =
        "Service Bus queue '{0}' could not be found";

    private const string ServiceBusTopicNotFoundCode = "ServiceBusTopicNotFound";

    private const string ServiceBusTopicNotFoundMessageTemplate =
        "Service Bus topic '{0}' could not be found";

    private const string ServiceBusSubscriptionNotFoundCode = "ServiceBusSubscriptionNotFound";

    private const string ServiceBusSubscriptionNotFoundMessageTemplate =
        "Service Bus subscription '{0}' could not be found";

    public static ServiceBusServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new ServiceBusResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

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
                @namespace, properties)
            {
                Tags = request.Tags
            };

            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, resource, true);

            return new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Created, resource, null,
                null);
        }

        existingNamespace.Tags = request.Tags;
        properties.UpdatedOn = DateTime.UtcNow;
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, properties);

        return new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Updated, existingNamespace,
            null, null);
    }

    public ControlPlaneOperationResult DeleteNamespace(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier serviceBusNamespaceIdentifier)
    {
        var existingNamespace =
            provider.GetAs<ServiceBusNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier,
                serviceBusNamespaceIdentifier.Value);
        if (existingNamespace == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, ServiceBusNamespaceNotFoundMessageTemplate,
                ServiceBusNamespaceNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, serviceBusNamespaceIdentifier.Value);
        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<ServiceBusNamespaceResource> GetNamespace(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(GetNamespace), "Getting namespace {0}",
            namespaceIdentifier);

        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(subscriptionIdentifier,
            resourceGroupIdentifier, namespaceIdentifier.Value);

        return existingNamespace == null
            ? new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.NotFound, null,
                ServiceBusNamespaceNotFoundMessageTemplate, ServiceBusNamespaceNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Success, existingNamespace,
                null, null);
    }

    public ControlPlaneOperationResult<ServiceBusQueueResource> CreateOrUpdateQueue(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier @namespace, string queueName, CreateOrUpdateServiceBusQueueRequest request)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());

        if (existingQueue == null)
        {
            var properties = ServiceBusQueueResourceProperties.From(request);
            var resource = new ServiceBusQueueResource(subscriptionIdentifier, resourceGroupIdentifier, @namespace,
                queueName, properties)
            {
                Properties =
                {
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                }
            };

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, queueName,
                @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant(), resource);

            return new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Created, resource, null,
                null);
        }

        ServiceBusQueueResourceProperties.UpdateFromRequest(existingQueue, request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, queueName, @namespace.Value,
            nameof(Subresource.Queues).ToLowerInvariant(), existingQueue);

        return new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Updated, existingQueue, null,
            null);
    }

    public ControlPlaneOperationResult DeleteQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier @namespace, string queueName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        if (existingQueue == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, ServiceBusQueueNotFoundMessageTemplate,
                ServiceBusQueueNotFoundCode);
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, queueName, @namespace.Value,
            nameof(Subresource.Queues).ToLowerInvariant());
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ServiceBusQueueResource> GetQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier @namespace, string queueName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, queueName, @namespace.Value, nameof(Subresource.Queues).ToLowerInvariant());
        return existingQueue == null
            ? new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.NotFound, null,
                ServiceBusQueueNotFoundMessageTemplate, ServiceBusQueueNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Success, existingQueue, null,
                null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        return resource.Type == "Microsoft.ServiceBus/namespaces"
            ? DeployServiceBusNamespace(resource)
            : DeployServiceBusQueue(resource);
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
            @namespace.Location!,
            ServiceBusNamespaceIdentifier.From(@namespace.Name),
            CreateOrUpdateServiceBusNamespaceRequest.From(resource));

        return result.Result;
    }

    public ControlPlaneOperationResult<ServiceBusTopicResource> GetTopic(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName)
    {
        var existingTopic = provider.GetSubresourceAs<ServiceBusTopicResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value,
            nameof(Subresource.Topics).ToLowerInvariant());

        return existingTopic == null
            ? new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.NotFound, null,
                ServiceBusTopicNotFoundMessageTemplate, ServiceBusTopicNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Success, existingTopic, null,
                null);
    }

    public ControlPlaneOperationResult<ServiceBusTopicResource> CreateOrUpdateTopic(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName, CreateOrUpdateServiceBusTopicRequest request)
    {
        var existingTopic = provider.GetSubresourceAs<ServiceBusTopicResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value,
            nameof(Subresource.Queues).ToLowerInvariant());

        if (existingTopic == null)
        {
            var properties = ServiceBusTopicResourceProperties.From(request);
            var resource = new ServiceBusTopicResource(subscriptionIdentifier, resourceGroupIdentifier,
                namespaceIdentifier, topicName, properties)
            {
                Properties =
                {
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                }
            };

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, topicName,
                namespaceIdentifier.Value, nameof(Subresource.Topics).ToLowerInvariant(), resource);

            return new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Created, resource, null,
                null);
        }

        ServiceBusTopicResourceProperties.UpdateFromRequest(existingTopic, request);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value,
            existingTopic);

        return new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Updated, existingTopic, null,
            null);
    }

    public static (OperationResult result, SubscriptionIdentifier? subscriptionIdentifier, ResourceGroupIdentifier?
        resourceGroupIdentifier) GetIdentifiersForParentResource(ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var dnsEntry = GlobalDnsEntries.GetEntry(ServiceBusService.UniqueName, namespaceIdentifier.Value);
        return dnsEntry == null
            ? (OperationResult.NotFound, null, null)
            : (OperationResult.Success, SubscriptionIdentifier.From(dnsEntry.Value.subscription),
                ResourceGroupIdentifier.From(dnsEntry.Value.resourceGroup));
    }

    public ControlPlaneOperationResult<ServiceBusSubscriptionResource> CreateOrUpdateSubscription(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName,
        CreateOrUpdateServiceBusSubscriptionRequest request)
    {
        var existingSubscription = provider.GetSubresourceAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, namespaceIdentifier.Value,
            nameof(Subresource.Subscriptions).ToLowerInvariant());

        if (existingSubscription == null)
        {
            var properties = ServiceBusSubscriptionResourceProperties.From(request);
            var resource = new ServiceBusSubscriptionResource(subscriptionIdentifier, resourceGroupIdentifier,
                namespaceIdentifier, subscriptionName, properties)
            {
                Properties =
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
                namespaceIdentifier.Value, nameof(Subresource.Subscriptions).ToLowerInvariant(), resource);

            return new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.Created, resource,
                null, null);
        }

        ServiceBusSubscriptionResourceProperties.UpdateFromRequest(existingSubscription, request);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value,
            existingSubscription);

        return new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.Updated,
            existingSubscription, null, null);
    }

    public ServiceBusEntityType GetEntityType(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string entityName, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            var queueOperation = GetQueue(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier,
                entityName);
            if (queueOperation.Result == OperationResult.Success)
            {
                return ServiceBusEntityType.Queue;
            }

            var topicOperation = GetTopic(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier,
                entityName);
            if (topicOperation.Result == OperationResult.Success)
            {
                return ServiceBusEntityType.Topic;
            }

            var subscriptionOperation = GetSubscription(subscriptionIdentifier, resourceGroupIdentifier,
                namespaceIdentifier, entityName);
            return subscriptionOperation.Result == OperationResult.Success
                ? ServiceBusEntityType.Subscription
                : ServiceBusEntityType.Unknown;
        }

        var xml = XDocument.Parse(content);
        if (xml.Descendants().Any(e => e.Name.LocalName == "QueueDescription"))
        {
            return ServiceBusEntityType.Queue;
        }

        if (xml.Descendants().Any(e => e.Name.LocalName == "TopicDescription"))
        {
            return ServiceBusEntityType.Topic;
        }

        return xml.Descendants().Any(e => e.Name.LocalName == "SubscriptionDescription")
            ? ServiceBusEntityType.Subscription
            : ServiceBusEntityType.Unknown;
    }

    internal ControlPlaneOperationResult<ServiceBusSubscriptionResource> GetSubscription(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName)
    {
        var existingSubscription = provider.GetSubresourceAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, namespaceIdentifier.Value,
            nameof(Subresource.Subscriptions).ToLowerInvariant());

        return existingSubscription == null
            ? new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.NotFound, null,
                ServiceBusSubscriptionNotFoundMessageTemplate, ServiceBusSubscriptionNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.Success,
                existingSubscription, null,
                null);
    }

    public OperationResult DeleteTopic(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName)
    {
        var existingQueue = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value,
            nameof(Subresource.Topics).ToLowerInvariant());
        if (existingQueue == null)
        {
            return OperationResult.NotFound;
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            namespaceIdentifier.Value, nameof(Subresource.Topics).ToLowerInvariant());
        return OperationResult.Deleted;
    }

    public OperationResult DeleteSubscription(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName)
    {
        var existingSubscription = provider.GetSubresourceAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, namespaceIdentifier.Value,
            nameof(Subresource.Subscriptions).ToLowerInvariant());
        if (existingSubscription == null)
        {
            return OperationResult.NotFound;
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
            namespaceIdentifier.Value, nameof(Subresource.Subscriptions).ToLowerInvariant());
        return OperationResult.Deleted;
    }

    public ControlPlaneOperationResult<ServiceBusNamespaceResource[]> ListNamespaces(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Resource == null || subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ServiceBusNamespaceResource[]>(OperationResult.NotFound, null,
                subscriptionOperation.Reason, subscriptionOperation.Code);
        }

        var resources =
            provider.ListAs<ServiceBusNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8)
                .ToImmutableArray();

        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(ListNamespaces), "Found {0} namespaces.",
            resources.Length);

        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));

        return new ControlPlaneOperationResult<ServiceBusNamespaceResource[]>(OperationResult.Success,
            filteredResources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<ServiceBusQueueResource[]> ListQueues(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier serviceBusNamespaceIdentifier)
    {
        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(ListQueues), "Listing queues for namespace {0}",
            serviceBusNamespaceIdentifier);

        var namespacesOperation =
            GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, serviceBusNamespaceIdentifier);
        if (namespacesOperation.Resource == null || namespacesOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ServiceBusQueueResource[]>(OperationResult.NotFound, null,
                namespacesOperation.Reason, namespacesOperation.Code);
        }

        var queues = provider.ListSubresourcesAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, serviceBusNamespaceIdentifier.Value,
            nameof(Subresource.Queues).ToLowerInvariant());

        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(ListQueues), "Found {0} queues.", queues.Length);

        return new ControlPlaneOperationResult<ServiceBusQueueResource[]>(OperationResult.Success,
            queues.ToArray(), null, null);
    }
}