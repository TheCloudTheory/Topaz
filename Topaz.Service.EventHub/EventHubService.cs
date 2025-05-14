using Topaz.Service.EventHub.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

public class EventHubService(ILogger logger) : IServiceDefinition
{
    private readonly ILogger logger = logger;
    public string Name => "Azure Event Hub";
    public static string LocalDirectoryPath => ".azure-event-hub";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new EventHubEndpoint(this.logger),
        new EventHubServiceEndpoint(this.logger),
        new EventHubAmqpEndpoint()
    ];
}