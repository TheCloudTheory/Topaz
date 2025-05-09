using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Subscription;

public sealed class SubscriptionService(ILogger logger) : IServiceDefinition
{
    private readonly ILogger logger = logger;

    public static string LocalDirectoryPath => ".subscription";

    public string Name => "Subscription";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new SubscriptionEndpoint(new ResourceProvider(this.logger), this.logger)
    ];
}
