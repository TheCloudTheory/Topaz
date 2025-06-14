using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

public sealed class SubscriptionService(ITopazLogger logger) : IServiceDefinition
{
    private readonly ITopazLogger _topazLogger = logger;

    public static string LocalDirectoryPath => ".subscription";

    public string Name => "Subscription";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new SubscriptionEndpoint(new ResourceProvider(this._topazLogger), this._topazLogger)
    ];
}
