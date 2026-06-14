using System.Text.Json;
using JetBrains.Annotations;
using Topaz.ResourceManager;

namespace Topaz.Service.LoadBalancer.Models.Requests;

[UsedImplicitly]
public class CreateOrUpdateLoadBalancerRequest
{
    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public ResourceSku? Sku { get; set; }
    public CreateOrUpdateLoadBalancerRequestProperties? Properties { get; set; }

    [UsedImplicitly]
    public class CreateOrUpdateLoadBalancerRequestProperties
    {
        public JsonElement? FrontendIPConfigurations { get; set; }
        public JsonElement? BackendAddressPools { get; set; }
        public JsonElement? LoadBalancingRules { get; set; }
        public JsonElement? Probes { get; set; }
        public JsonElement? InboundNatRules { get; set; }
        public JsonElement? OutboundRules { get; set; }
    }
}
