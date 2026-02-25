using Topaz.Service.Shared;
using Topaz.Service.Subscription.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

public sealed class SubscriptionService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(".subscription", "{subscriptionId}");
    
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "subscription";

    public string Name => "Subscription";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new GetSubscriptionEndpoint(logger),
        new CreateSubscriptionEndpoint(logger),
        new ListSubscriptionsEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}
