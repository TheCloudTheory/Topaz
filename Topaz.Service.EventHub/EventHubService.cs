using Topaz.Service.EventHub.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

public class EventHubService(ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "eventhub";
    public string Name => "Azure Event Hub";
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-event-hub");
    public static IReadOnlyCollection<string>? Subresources => ["hubs"];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new EventHubEndpoint(logger),
        new EventHubServiceEndpoint(logger),
        new EventHubAmqpEndpoint()
    ];
}