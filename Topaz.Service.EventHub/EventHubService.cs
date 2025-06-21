using Topaz.Service.EventHub.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

public class EventHubService(ITopazLogger logger) : IServiceDefinition
{
    public string Name => "Azure Event Hub";
    public static string LocalDirectoryPath => ".azure-event-hub";
    public static IReadOnlyCollection<string>? Subresources => ["hubs"];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new EventHubEndpoint(logger),
        new EventHubServiceEndpoint(logger),
        new EventHubAmqpEndpoint()
    ];
}