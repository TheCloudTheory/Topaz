using Topaz.Service.ServiceBus.Models;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus;

internal sealed partial class ServiceBusServiceControlPlane
{
    private static readonly string AuthRules = nameof(Subresource.AuthorizationRules).ToLowerInvariant();
    private static string QueueAuthRuleParentId(ServiceBusNamespaceIdentifier ns, string queue) => $"{ns.Value}/queues/{queue}";
    private static string TopicAuthRuleParentId(ServiceBusNamespaceIdentifier ns, string topic) => $"{ns.Value}/topics/{topic}";

    private ServiceBusAuthorizationRuleResource BuildAuthRule(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
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
        return new ServiceBusAuthorizationRuleResource(subscriptionIdentifier, resourceGroupIdentifier, ruleName, props, armIdSuffix);
    }
    
    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> CreateOrUpdateNamespaceAuthorizationRule(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string ruleName,
        CreateOrUpdateServiceBusAuthorizationRuleRequest request)
    {
        var existing = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(subscriptionIdentifier, resourceGroupIdentifier, ruleName, namespaceIdentifier.Value, AuthRules);
        var resource = BuildAuthRule(subscriptionIdentifier, resourceGroupIdentifier, ruleName, request, $"namespaces/{namespaceIdentifier.Value}", existing);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, ruleName, namespaceIdentifier.Value, AuthRules, resource);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(
            existing == null ? OperationResult.Created : OperationResult.Updated, resource);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource> GetNamespaceAuthorizationRule(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(subscriptionIdentifier, resourceGroupIdentifier, ruleName, namespaceIdentifier.Value, AuthRules);
        return rule == null
            ? new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound")
            : new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource>(OperationResult.Success, rule);
    }

    public ControlPlaneOperationResult DeleteNamespaceAuthorizationRule(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string ruleName)
    {
        if (provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(subscriptionIdentifier, resourceGroupIdentifier, ruleName, namespaceIdentifier.Value, AuthRules) == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                $"Authorization rule '{ruleName}' not found.", "AuthorizationRuleNotFound");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, ruleName, namespaceIdentifier.Value, AuthRules);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]> ListNamespaceAuthorizationRules(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var rules = provider.ListSubresourcesAs<ServiceBusAuthorizationRuleResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier.Value, AuthRules);
        return new ControlPlaneOperationResult<ServiceBusAuthorizationRuleResource[]>(OperationResult.Success, rules);
    }

    public ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse> ListNamespaceAuthorizationRuleKeys(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string ruleName)
    {
        var rule = provider.GetSubresourceAs<ServiceBusAuthorizationRuleResource>(subscriptionIdentifier, resourceGroupIdentifier, ruleName, namespaceIdentifier.Value, AuthRules);
        if (rule == null)
        {
            return new ControlPlaneOperationResult<Models.Responses.ListKeysServiceBusNamespaceResponse>(
                OperationResult.NotFound, null, $"Authorization rule '{ruleName}' not found.",
                "AuthorizationRuleNotFound");
        }
        
        var keys = Models.Responses.ListKeysServiceBusNamespaceResponse.For(namespaceIdentifier.Value, ruleName, rule.Properties.PrimaryKey, rule.Properties.SecondaryKey);
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