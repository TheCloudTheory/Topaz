using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Subscription.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

public sealed class SubscriptionService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(".subscription", "{subscriptionId}");
    
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "subscription";

    public string Name => "Subscription";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new GetSubscriptionEndpoint(eventPipeline, logger),
        new CreateSubscriptionEndpoint(eventPipeline, logger),
        new ListSubscriptionsEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
