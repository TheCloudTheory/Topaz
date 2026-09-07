using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics.SystemTopics;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

public sealed class EventGridSystemTopicService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;

    public static string LocalDirectoryPath =>
        Path.Combine(ResourceGroupService.LocalDirectoryPath, ".event-grid-system-topic");

    public static IReadOnlyCollection<string>? Subresources { get; } =
    [
        nameof(Subresource.TopicEventSubscriptions).ToLowerInvariant(),
        nameof(Subresource.Events).ToLowerInvariant(),
        nameof(Subresource.ValidatedSubscriptions).ToLowerInvariant(),
    ];

    public static string UniqueName => "eventgrid-system-topic";
    public string Name => "Event Grid System Topics";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints { get; } =
    [
        new CreateOrUpdateEventGridSystemTopicEndpoint(eventPipeline, logger),
        new GetEventGridSystemTopicEndpoint(eventPipeline, logger),
        new DeleteEventGridSystemTopicEndpoint(eventPipeline, logger),
        new ListEventGridSystemTopicByResourceGroupEndpoint(eventPipeline, logger),
        new ListEventGridSystemTopicBySubscriptionEndpoint(eventPipeline, logger),
        new UpdateEventGridSystemTopicEndpoint(eventPipeline, logger)
    ];

    public void Register()
    {
        var dataPlane = EventGridDataPlane.New(EventGridTopicControlPlane.New(eventPipeline, logger), logger);
        
        eventPipeline.RegisterHandler<EventGridEventPublishedEvent>(EventGridEventPublishedEvent.EventName,
            data => dataPlane.PublishEvent(data!.Data));
    }
}