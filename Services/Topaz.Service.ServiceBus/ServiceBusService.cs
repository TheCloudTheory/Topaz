using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ServiceBus.Endpoints;
using Topaz.Service.ServiceBus.Endpoints.DataPlane;
using Topaz.Service.ServiceBus.Endpoints.Namespace;
using Topaz.Service.ServiceBus.Endpoints.Queue;
using Topaz.Service.ServiceBus.Endpoints.Topic;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

public sealed class ServiceBusService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "servicebus";
    public string Name => "Azure Service Bus";
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".service-bus");

    public static IReadOnlyCollection<string> Subresources =>
    [
        nameof(Subresource.Queues).ToLowerInvariant(), nameof(Subresource.Topics).ToLowerInvariant(),
        nameof(Subresource.Subscriptions).ToLowerInvariant(), nameof(Subresource.NetworkRuleSets).ToLowerInvariant(),
        nameof(Subresource.Rules).ToLowerInvariant()
    ];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ServiceBusEndpoint(),
        new GetServiceBusNamespaceEndpoint(eventPipeline, logger),
        new CreateOrUpdateServiceBusNamespaceEndpoint(eventPipeline, logger),
        new GetServiceBusQueueEndpoint(eventPipeline, logger),
        new GetEntityDataPlaneEndpoint(eventPipeline, logger),
        new CreateOrUpdateEntityDataPlaneEndpoint(eventPipeline, logger),
        new DeleteEntityDataPlaneEndpoint(eventPipeline, logger),
        new ListSubscriptionsDataPlaneEndpoint(eventPipeline, logger),
        new GetTopicDataPlaneEndpoint(eventPipeline, logger),
        new CreateOrUpdateTopicDataPlaneEndpoint(eventPipeline, logger),
        new GetSubscriptionDataPlaneEndpoint(eventPipeline, logger),
        new CreateOrUpdateSubscriptionDataPlaneEndpoint(eventPipeline, logger),
        new DeleteSubscriptionDataPlaneEndpoint(eventPipeline, logger),
        new GetTopicSubscriptionDataPlaneEndpoint(eventPipeline, logger),
        new CreateOrUpdateTopicSubscriptionDataPlaneEndpoint(eventPipeline, logger),
        new DeleteTopicSubscriptionDataPlaneEndpoint(eventPipeline, logger),
        new CreateOrUpdateRuleDataPlaneEndpoint(eventPipeline, logger),
        new GetRuleDataPlaneEndpoint(eventPipeline, logger),
        new DeleteRuleDataPlaneEndpoint(eventPipeline, logger),
        new ListRulesDataPlaneEndpoint(eventPipeline, logger),
        new ListServiceBusNamespacesEndpoint(eventPipeline, logger),
        new ListServiceBusNamespacesBySubscriptionEndpoint(eventPipeline, logger),
        new DeleteServiceBusNamespaceEndpoint(eventPipeline, logger),
        new GetServiceBusNamespaceNetworkRuleSetEndpoint(eventPipeline, logger),
        new ListServiceBusQueuesEndpoint(eventPipeline, logger),
        new CreateUpdateServiceBusQueueEndpoint(eventPipeline, logger),
        new DeleteServiceBusQueueEndpoint(eventPipeline, logger),
        new ListServiceBusTopicsEndpoint(eventPipeline, logger),
        new CreateOrUpdateServiceBusTopicEndpoint(eventPipeline, logger),
        new GetServiceBusTopicEndpoint(eventPipeline, logger),
        new DeleteServiceBusTopicEndpoint(eventPipeline, logger),
        new CreateOrUpdateServiceBusSubscriptionEndpoint(eventPipeline, logger),
        new GetServiceBusSubscriptionEndpoint(eventPipeline, logger),
        new ListServiceBusSubscriptionsEndpoint(eventPipeline, logger),
        new CreateOrUpdateServiceBusRuleEndpoint(eventPipeline, logger),
        new GetServiceBusRuleEndpoint(eventPipeline, logger),
        new DeleteServiceBusRuleEndpoint(eventPipeline, logger),
        new ListServiceBusRulesEndpoint(eventPipeline, logger),
        new ListKeysServiceBusNamespaceEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}