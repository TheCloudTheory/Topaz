using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Endpoints.ControlPlane.Namespace;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

public sealed class EventGridService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".event-grid");
    public static IReadOnlyCollection<string>? Subresources { get; } = [];
    public static string UniqueName => "eventgrid";
    public string Name => "Event Grid";
    public IReadOnlyCollection<IEndpointDefinition> Endpoints { get; } = [
        new CreateOrUpdateEventGridNamespaceEndpoint(eventPipeline, logger),
        new GetEventGridNamespaceEndpoint(eventPipeline, logger),
        new DeleteEventGridNamespaceEndpoint(eventPipeline, logger),
        new UpdateEventGridNamespaceEndpoint(eventPipeline, logger)
    ];
}