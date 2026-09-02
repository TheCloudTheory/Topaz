using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics;
using Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics.TopicSubscriptions;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

public sealed class EventGridTopicService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;

    public static string LocalDirectoryPath =>
        Path.Combine(ResourceGroupService.LocalDirectoryPath, ".event-grid-topic");

    public static IReadOnlyCollection<string>? Subresources { get; } =
    [
        nameof(Subresource.SharedAccessKeys).ToLowerInvariant(),
        nameof(Subresource.TopicEventSubscriptions).ToLowerInvariant(),
    ];

    public static string UniqueName => "eventgrid-topic";
    public string Name => "Event Grid Topics";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints { get; } =
    [
        new CreateOrUpdateEventGridTopicEndpoint(eventPipeline, logger),
        new GetEventGridTopicEndpoint(eventPipeline, logger),
        new DeleteEventGridTopicEndpoint(eventPipeline, logger),
        new UpdateEventGridTopicEndpoint(eventPipeline, logger),
        new RegenerateEventGridTopicKeyEndpoint(eventPipeline, logger),
        new ListEventGridTopicByResourceGroupEndpoint(eventPipeline, logger),
        new ListEventGridTopicBySubscriptionEndpoint(eventPipeline, logger),
        new ListEventGridTopicSharedAccessKeysEndpoint(eventPipeline, logger),
        new ListEventGridTopicEventTypesEndpoint(eventPipeline, logger),
        new CreateOrUpdateEventGridTopicSubscriptionEndpoint(eventPipeline, logger),
        new GetEventGridTopicSubscriptionEndpoint(eventPipeline, logger),
        new DeleteEventGridTopicSubscriptionEndpoint(eventPipeline, logger),
        new ListEventGridTopicSubscriptionsEndpoint(eventPipeline, logger),
        new UpdateEventGridTopicSubscriptionEndpoint(eventPipeline, logger),
        new GetEventGridTopicSubscriptionUrlEndpoint(eventPipeline, logger),
        new GetEventGridTopicSubscriptionDeliveryAttributesEndpoint(eventPipeline, logger)
    ];
}