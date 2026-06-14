using JetBrains.Annotations;

namespace Topaz.Service.LoadBalancer.Models.Requests;

[UsedImplicitly]
public class UpdateLoadBalancerTagsRequest
{
    public IDictionary<string, string>? Tags { get; set; }
}
