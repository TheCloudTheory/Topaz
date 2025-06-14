using Topaz.Service.EventHub.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

public class EventHubService(ITopazLogger logger) : IServiceDefinition
{
    private readonly ITopazLogger _topazLogger = logger;
    public string Name => "Azure Event Hub";
    public static string LocalDirectoryPath => ".azure-event-hub";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new EventHubEndpoint(this._topazLogger),
        new EventHubServiceEndpoint(this._topazLogger),
        new EventHubAmqpEndpoint()
    ];
}