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
                Tags = request.Tags,
                Sku = request.Sku
            };

            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, resource, true);

            CreateOrUpdateNamespaceAuthorizationRule(subscriptionIdentifier, resourceGroupIdentifier, @namespace,
                "RootManageSharedAccessKey",
                new CreateOrUpdateServiceBusAuthorizationRuleRequest
                {
                    Properties = new CreateOrUpdateServiceBusAuthorizationRuleRequestProperties
                    {
                        Rights = ["Listen", "Manage", "Send"]
                    }
                });

            return new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Created, resource);
        }

        properties.CreatedOn = existingNamespace.Properties.CreatedOn;
        properties.UpdatedOn = DateTime.UtcNow;
        var updatedResource = new ServiceBusNamespaceResource(subscriptionIdentifier, resourceGroupIdentifier,
            request.Location ?? existingNamespace.Location ?? location,
            @namespace, properties)
        {
            Tags = request.Tags ?? existingNamespace.Tags,
            Sku = request.Sku ?? existingNamespace.Sku
        };
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, @namespace.Value, updatedResource);

        return new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Updated, updatedResource);
    }

    public ControlPlaneOperationResult DeleteNamespace(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier serviceBusNamespaceIdentifier)
    {
        var existingNamespace =
            provider.GetAs<ServiceBusNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier,
                serviceBusNamespaceIdentifier.Value);
        if (existingNamespace == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                string.Format(ServiceBusNamespaceNotFoundMessageTemplate, serviceBusNamespaceIdentifier),
                ServiceBusNamespaceNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, serviceBusNamespaceIdentifier.Value);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
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
                string.Format(ServiceBusNamespaceNotFoundMessageTemplate, namespaceIdentifier), ServiceBusNamespaceNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusNamespaceResource>(OperationResult.Success, existingNamespace);
    }

    public ControlPlaneOperationResult<ServiceBusNetworkRuleSetSubresource> GetNetworkRuleSet(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string networkRuleSetName)
    {
        var existingNamespace = GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (existingNamespace.Result == OperationResult.NotFound || existingNamespace.Resource == null)
        {
            return new ControlPlaneOperationResult<ServiceBusNetworkRuleSetSubresource>(OperationResult.NotFound, null,
                string.Format(ServiceBusNamespaceNotFoundMessageTemplate, namespaceIdentifier),
                ServiceBusNamespaceNotFoundCode);
        }

        var networkRuleSet = provider.GetSubresourceAs<ServiceBusNetworkRuleSetSubresource>(subscriptionIdentifier,
            resourceGroupIdentifier, networkRuleSetName, namespaceIdentifier.Value,
            nameof(Subresource.NetworkRuleSets).ToLowerInvariant());

        if (networkRuleSet != null)
        {
            return new ControlPlaneOperationResult<ServiceBusNetworkRuleSetSubresource>(OperationResult.Success,
                networkRuleSet);
        }

        var defaultProperties = ServiceBusNetworkRuleSetSubresourceProperties.Default();
        var created = new ServiceBusNetworkRuleSetSubresource(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier, networkRuleSetName, defaultProperties);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, networkRuleSetName,
            namespaceIdentifier.Value, nameof(Subresource.NetworkRuleSets).ToLowerInvariant(), created);

        return new ControlPlaneOperationResult<ServiceBusNetworkRuleSetSubresource>(OperationResult.Success, created);
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

            ServiceBusService.OnQueueCreated?.Invoke(queueName);

            return new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Created, resource);
        }

        ServiceBusQueueResourceProperties.UpdateFromRequest(existingQueue, request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, queueName, @namespace.Value,
            nameof(Subresource.Queues).ToLowerInvariant(), existingQueue);

        return new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Updated, existingQueue);
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
            : new ControlPlaneOperationResult<ServiceBusQueueResource>(OperationResult.Success, existingQueue);
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
                string.Format(ServiceBusTopicNotFoundMessageTemplate, topicName), ServiceBusTopicNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Success, existingTopic);
    }

    public ControlPlaneOperationResult<ServiceBusTopicResource> CreateOrUpdateTopic(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName, CreateOrUpdateServiceBusTopicRequest request)
    {
        var existingTopic = provider.GetSubresourceAs<ServiceBusTopicResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value,
            nameof(Subresource.Topics).ToLowerInvariant());

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

            return new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Created, resource);
        }

        ServiceBusTopicResourceProperties.UpdateFromRequest(existingTopic, request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            namespaceIdentifier.Value, nameof(Subresource.Topics).ToLowerInvariant(), existingTopic);

        return new ControlPlaneOperationResult<ServiceBusTopicResource>(OperationResult.Updated, existingTopic);
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

    private static string SubscriptionsParentId(ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName) =>
        $"{namespaceIdentifier.Value}/topics/{topicName}";

    public ControlPlaneOperationResult<ServiceBusSubscriptionResource> CreateOrUpdateSubscription(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName,
        CreateOrUpdateServiceBusSubscriptionRequest request,
        string topicName)
    {
        var parentId = SubscriptionsParentId(namespaceIdentifier, topicName);
        var existingSubscription = provider.GetSubresourceAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, parentId,
            nameof(Subresource.Subscriptions).ToLowerInvariant());

        if (existingSubscription == null)
        {
            var properties = ServiceBusSubscriptionResourceProperties.From(request);
            var resource = new ServiceBusSubscriptionResource(subscriptionIdentifier, resourceGroupIdentifier,
                namespaceIdentifier, subscriptionName, properties, topicName)
            {
                Properties =
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
                parentId, nameof(Subresource.Subscriptions).ToLowerInvariant(), resource);

            CreateOrUpdateRule(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier,
                topicName, subscriptionName, "$Default", ServiceBusRuleResourceProperties.DefaultTrueFilter());

            return new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.Created, resource);
        }

        ServiceBusSubscriptionResourceProperties.UpdateFromRequest(existingSubscription, request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
            parentId, nameof(Subresource.Subscriptions).ToLowerInvariant(), existingSubscription);

        return new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.Updated,
            existingSubscription);
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

            return ServiceBusEntityType.Unknown;
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
        ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, string subscriptionName)
    {
        var existingSubscription = provider.GetSubresourceAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, SubscriptionsParentId(namespaceIdentifier, topicName),
            nameof(Subresource.Subscriptions).ToLowerInvariant());

        return existingSubscription == null
            ? new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.NotFound, null,
                ServiceBusSubscriptionNotFoundMessageTemplate, ServiceBusSubscriptionNotFoundCode)
            : new ControlPlaneOperationResult<ServiceBusSubscriptionResource>(OperationResult.Success,
                existingSubscription);
    }

    internal ControlPlaneOperationResult<ServiceBusSubscriptionResource[]> ListSubscriptions(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName)
    {
        var subscriptions = provider.ListSubresourcesShallowAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, SubscriptionsParentId(namespaceIdentifier, topicName),
            nameof(Subresource.Subscriptions).ToLowerInvariant());

        return new ControlPlaneOperationResult<ServiceBusSubscriptionResource[]>(OperationResult.Success,
            subscriptions);
    }

    public ControlPlaneOperationResult DeleteTopic(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName)
    {
        var existingTopic = provider.GetSubresourceAs<ServiceBusQueueResource>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, namespaceIdentifier.Value,
            nameof(Subresource.Topics).ToLowerInvariant());
        if (existingTopic == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, string.Format(ServiceBusTopicNotFoundMessageTemplate, topicName), ServiceBusTopicNotFoundCode);
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            namespaceIdentifier.Value, nameof(Subresource.Topics).ToLowerInvariant());
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public OperationResult DeleteSubscription(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, string subscriptionName)
    {
        var parentId = SubscriptionsParentId(namespaceIdentifier, topicName);
        var existingSubscription = provider.GetSubresourceAs<ServiceBusSubscriptionResource>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, parentId,
            nameof(Subresource.Subscriptions).ToLowerInvariant());
        if (existingSubscription == null)
        {
            return OperationResult.NotFound;
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
            parentId, nameof(Subresource.Subscriptions).ToLowerInvariant());
        return OperationResult.Deleted;
    }

    public ControlPlaneOperationResult<ServiceBusNamespaceResource[]> ListNamespacesBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider
            .ListAs<ServiceBusNamespaceResource>(subscriptionIdentifier, null, null, 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(ListNamespacesBySubscription),
            "Found {0} namespaces in subscription {1}.", resources.Length, subscriptionIdentifier);

        return new ControlPlaneOperationResult<ServiceBusNamespaceResource[]>(OperationResult.Success, resources);
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
            filteredResources.ToArray());
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
            queues.ToArray());
    }

    public ControlPlaneOperationResult<ServiceBusTopicResource[]> ListTopics(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier serviceBusNamespaceIdentifier)
    {
        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(ListTopics), "Listing topics for namespace {0}",
            serviceBusNamespaceIdentifier);

        var namespacesOperation =
            GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, serviceBusNamespaceIdentifier);
        if (namespacesOperation.Resource == null || namespacesOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ServiceBusTopicResource[]>(OperationResult.NotFound, null,
                namespacesOperation.Reason, namespacesOperation.Code);
        }

        var queues = provider.ListSubresourcesAs<ServiceBusTopicResource>(subscriptionIdentifier,
            resourceGroupIdentifier, serviceBusNamespaceIdentifier.Value,
            nameof(Subresource.Topics).ToLowerInvariant());

        logger.LogDebug(nameof(ServiceBusServiceControlPlane), nameof(ListTopics), "Found {0} queues.", queues.Length);

        return new ControlPlaneOperationResult<ServiceBusTopicResource[]>(OperationResult.Success,
            queues.ToArray());
    }

    private static string RulesParentId(ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, string subscriptionName) =>
        $"{namespaceIdentifier.Value}/topics/{topicName}/subscriptions/{subscriptionName}";

    public ControlPlaneOperationResult<ServiceBusRuleResource> CreateOrUpdateRule(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName,
        string subscriptionName,
        string ruleName,
        ServiceBusRuleResourceProperties properties)
    {
        var parentId = RulesParentId(namespaceIdentifier, topicName, subscriptionName);
        var existing = provider.GetSubresourceAs<ServiceBusRuleResource>(subscriptionIdentifier,
            resourceGroupIdentifier, ruleName, parentId, nameof(Subresource.Rules).ToLowerInvariant());

        var resource = new ServiceBusRuleResource(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier, topicName, subscriptionName, ruleName, properties);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, ruleName,
            parentId, nameof(Subresource.Rules).ToLowerInvariant(), resource);

        return new ControlPlaneOperationResult<ServiceBusRuleResource>(
            existing == null ? OperationResult.Created : OperationResult.Updated, resource);
    }

    public ControlPlaneOperationResult<ServiceBusRuleResource> GetRule(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName,
        string subscriptionName,
        string ruleName)
    {
        var parentId = RulesParentId(namespaceIdentifier, topicName, subscriptionName);
        var rule = provider.GetSubresourceAs<ServiceBusRuleResource>(subscriptionIdentifier,
            resourceGroupIdentifier, ruleName, parentId, nameof(Subresource.Rules).ToLowerInvariant());

        return rule == null
            ? new ControlPlaneOperationResult<ServiceBusRuleResource>(OperationResult.NotFound, null,
                $"Rule '{ruleName}' not found.", "RuleNotFound")
            : new ControlPlaneOperationResult<ServiceBusRuleResource>(OperationResult.Success, rule);
    }

    public OperationResult DeleteRule(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName,
        string subscriptionName,
        string ruleName)
    {
        var parentId = RulesParentId(namespaceIdentifier, topicName, subscriptionName);
        var existing = provider.GetSubresourceAs<ServiceBusRuleResource>(subscriptionIdentifier,
            resourceGroupIdentifier, ruleName, parentId, nameof(Subresource.Rules).ToLowerInvariant());
        if (existing == null)
            return OperationResult.NotFound;

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, ruleName,
            parentId, nameof(Subresource.Rules).ToLowerInvariant());
        return OperationResult.Deleted;
    }

    public ControlPlaneOperationResult<ServiceBusRuleResource[]> ListRules(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName,
        string subscriptionName)
    {
        var parentId = RulesParentId(namespaceIdentifier, topicName, subscriptionName);
        var rules = provider.ListSubresourcesAs<ServiceBusRuleResource>(subscriptionIdentifier,
            resourceGroupIdentifier, parentId, nameof(Subresource.Rules).ToLowerInvariant());

        return new ControlPlaneOperationResult<ServiceBusRuleResource[]>(OperationResult.Success, rules);
    }

    // ── Authorization Rules ────────────────────────────────────────────────────

    private static readonly string AuthRules = nameof(Subresource.AuthorizationRules).ToLowerInvariant();
    private static string QueueAuthRuleParentId(ServiceBusNamespaceIdentifier ns, string queue) => $"{ns.Value}/queues/{queue}";
    private static string TopicAuthRuleParentId(ServiceBusNamespaceIdentifier ns, string topic) => $"{ns.Value}/topics/{topic}";

    private ServiceBusAuthorizationRuleResource BuildAuthRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string ruleName, CreateOrUpdateServiceBusAuthorizationRuleRequest request,
        string armIdSuffix,
        ServiceBusAuthorizationRuleResource? existing)
    {
        if (existing != null)
        {
            ServiceBusAuthorizationRuleResourceProperties.UpdateFromRequest(existing, request);
            return existing;
        }
        var rights = request.Properties?.Rights ?? ["Listen", "Send"];
        var props = ServiceBusAuthorizationRuleResourceProperties.Create(ruleName, rights);
        return new ServiceBusAuthorizationRuleResource(sub, rg, ruleName, props, armIdSuffix);
    }

    // Namespace
    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> CreateOrUpdateNamespaceAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string ruleName,
        CreateOrUpdateServiceBusAuthorizationRuleRequest request)
    {
        var existing = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, ns.Value, AuthRules);
        var resource = BuildAuthRule(sub, rg, ruleName, request, $"namespaces/{ns.Value}", existing);
        provider.CreateOrUpdateSubresource(sub, rg, ruleName, ns.Value, AuthRules, resource);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(
            existing == null ? OperationResult.Created : OperationResult.Updated, resource);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> GetNamespaceAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, ns.Value, AuthRules);
        return rule == null
            ? new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound")
            : new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.Success, rule);
    }

    public ControlPlaneOperationResult DeleteNamespaceAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string ruleName)
    {
        if (provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, ns.Value, AuthRules) == null)
            return new ControlPlaneOperationResult(OperationResult.NotFound, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        provider.DeleteSubresource(sub, rg, ruleName, ns.Value, AuthRules);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]> ListNamespaceAuthorizationRules(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg, ServiceBusNamespaceIdentifier ns)
    {
        var rules = provider.ListSubresourcesAs<ServiceBusAuthorizationRuleResource>(sub, rg, ns.Value, AuthRules);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]>(OperationResult.Success, rules);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> ListNamespaceAuthorizationRuleKeys(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, ns.Value, AuthRules);
        if (rule == null)
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(ns.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
        return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.Success, keys);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> RegenerateNamespaceAuthorizationRuleKeys(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string ruleName,
        RegenerateServiceBusAuthorizationRuleKeysRequest request)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, ns.Value, AuthRules);
        if (rule == null)
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        RegenerateKey(rule.Properties, request.KeyType);
        provider.CreateOrUpdateSubresource(sub, rg, ruleName, ns.Value, AuthRules, rule);
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(ns.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
        return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.Success, keys);
    }

    // Queue
    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> CreateOrUpdateQueueAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string queueName, string ruleName,
        CreateOrUpdateServiceBusAuthorizationRuleRequest request)
    {
        var parentId = QueueAuthRuleParentId(ns, queueName);
        var existing = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, parentId, AuthRules);
        var resource = BuildAuthRule(sub, rg, ruleName, request, $"namespaces/{ns.Value}/queues/{queueName}", existing);
        provider.CreateOrUpdateSubresource(sub, rg, ruleName, parentId, AuthRules, resource);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(
            existing == null ? OperationResult.Created : OperationResult.Updated, resource);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> GetQueueAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string queueName, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, QueueAuthRuleParentId(ns, queueName), AuthRules);
        return rule == null
            ? new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound")
            : new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.Success, rule);
    }

    public ControlPlaneOperationResult DeleteQueueAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string queueName, string ruleName)
    {
        var parentId = QueueAuthRuleParentId(ns, queueName);
        if (provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, parentId, AuthRules) == null)
            return new ControlPlaneOperationResult(OperationResult.NotFound, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        provider.DeleteSubresource(sub, rg, ruleName, parentId, AuthRules);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]> ListQueueAuthorizationRules(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string queueName)
    {
        var rules = provider.ListSubresourcesAs<ServiceBusAuthorizationRuleResource>(sub, rg, QueueAuthRuleParentId(ns, queueName), AuthRules);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]>(OperationResult.Success, rules);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> ListQueueAuthorizationRuleKeys(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string queueName, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, QueueAuthRuleParentId(ns, queueName), AuthRules);
        if (rule == null)
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(ns.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
        return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.Success, keys);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> RegenerateQueueAuthorizationRuleKeys(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string queueName, string ruleName,
        RegenerateServiceBusAuthorizationRuleKeysRequest request)
    {
        var parentId = QueueAuthRuleParentId(ns, queueName);
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, parentId, AuthRules);
        if (rule == null)
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        RegenerateKey(rule.Properties, request.KeyType);
        provider.CreateOrUpdateSubresource(sub, rg, ruleName, parentId, AuthRules, rule);
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(ns.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
        return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.Success, keys);
    }

    // Topic
    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> CreateOrUpdateTopicAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string topicName, string ruleName,
        CreateOrUpdateServiceBusAuthorizationRuleRequest request)
    {
        var parentId = TopicAuthRuleParentId(ns, topicName);
        var existing = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, parentId, AuthRules);
        var resource = BuildAuthRule(sub, rg, ruleName, request, $"namespaces/{ns.Value}/topics/{topicName}", existing);
        provider.CreateOrUpdateSubresource(sub, rg, ruleName, parentId, AuthRules, resource);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(
            existing == null ? OperationResult.Created : OperationResult.Updated, resource);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> GetTopicAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string topicName, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, TopicAuthRuleParentId(ns, topicName), AuthRules);
        return rule == null
            ? new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound")
            : new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.Success, rule);
    }

    public ControlPlaneOperationResult DeleteTopicAuthorizationRule(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string topicName, string ruleName)
    {
        var parentId = TopicAuthRuleParentId(ns, topicName);
        if (provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, parentId, AuthRules) == null)
            return new ControlPlaneOperationResult(OperationResult.NotFound, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        provider.DeleteSubresource(sub, rg, ruleName, parentId, AuthRules);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]> ListTopicAuthorizationRules(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string topicName)
    {
        var rules = provider.ListSubresourcesAs<ServiceBusAuthorizationRuleResource>(sub, rg, TopicAuthRuleParentId(ns, topicName), AuthRules);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]>(OperationResult.Success, rules);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> ListTopicAuthorizationRuleKeys(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string topicName, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, TopicAuthRuleParentId(ns, topicName), AuthRules);
        if (rule == null)
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(ns.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
        return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.Success, keys);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> RegenerateTopicAuthorizationRuleKeys(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        ServiceBusNamespaceIdentifier ns, string topicName, string ruleName,
        RegenerateServiceBusAuthorizationRuleKeysRequest request)
    {
        var parentId = TopicAuthRuleParentId(ns, topicName);
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(sub, rg, ruleName, parentId, AuthRules);
        if (rule == null)
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        RegenerateKey(rule.Properties, request.KeyType);
        provider.CreateOrUpdateSubresource(sub, rg, ruleName, parentId, AuthRules, rule);
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(ns.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
        return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(OperationResult.Success, keys);
    }

    private static void RegenerateKey(ServiceBusAuthorizationRuleResourceProperties props, string? keyType)
    {
        if (keyType == "SecondaryKey")
            props.SecondaryKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        else
            props.PrimaryKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}