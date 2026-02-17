using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class SubscriptionAuthorizationService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".authorization");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "subscription-authorization";
    public string Name => "Subscription Authorization";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];
}
