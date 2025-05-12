using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

public sealed class SubscriptionService(ILogger logger) : IServiceDefinition
{
    private readonly ILogger logger = logger;

    public static string LocalDirectoryPath => ".subscription";

    public string Name => "Subscription";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new SubscriptionEndpoint(new ResourceProvider(this.logger), this.logger)
    ];
}
