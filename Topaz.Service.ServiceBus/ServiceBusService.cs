using Topaz.Service.ServiceBus.Endpoints;
using Topaz.Service.Shared;

namespace Topaz.Service.ServiceBus;

public sealed class ServiceBusService : IServiceDefinition
{
    public string Name => "Azure Service Bus";
    public static string LocalDirectoryPath => ".service-bus";
    public static IReadOnlyCollection<string>? Subresources => [nameof(Subresource.Queues).ToLowerInvariant(), nameof(Subresource.Topics).ToLowerInvariant()];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ServiceBusEndpoint()
    ];
}