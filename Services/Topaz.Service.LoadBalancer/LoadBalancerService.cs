using Topaz.EventPipeline;
using Topaz.Service.LoadBalancer.Endpoints.LoadBalancers;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.LoadBalancer;

public sealed class LoadBalancerService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".load-balancer");
    
    public static IReadOnlyCollection<string> Subresources => Array.Empty<string>();
    public static string UniqueName => "load-balancer";

    public string Name => "Load Balancer";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateOrUpdateLoadBalancerEndpoint(eventPipeline, logger),
        new GetLoadBalancerEndpoint(eventPipeline, logger),
        new DeleteLoadBalancerEndpoint(eventPipeline, logger),
        new UpdateLoadBalancerTagsEndpoint(eventPipeline, logger),
        new ListLoadBalancersByResourceGroupEndpoint(eventPipeline, logger),
        new ListLoadBalancersBySubscriptionEndpoint(eventPipeline, logger)
    ];
}
