using System.Text.Json;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.LoadBalancer.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.LoadBalancer.Models;

public sealed class LoadBalancerResourceProperties
{
    public ResourceSku? Sku { get; set; }
    public JsonElement? FrontendIPConfigurations { get; set; }
    public JsonElement? BackendAddressPools { get; set; }
    public JsonElement? LoadBalancingRules { get; set; }
    public JsonElement? Probes { get; set; }
    public JsonElement? InboundNatRules { get; set; }
    public JsonElement? OutboundRules { get; set; }
    public string ProvisioningState => "Succeeded";

    public static LoadBalancerResourceProperties FromRequest(CreateOrUpdateLoadBalancerRequest request)
    {
        return new LoadBalancerResourceProperties
        {
            Sku = request.Sku,
            FrontendIPConfigurations = request.Properties?.FrontendIPConfigurations != null
                ? JsonSerializer.SerializeToElement(request.Properties.FrontendIPConfigurations, GlobalSettings.JsonOptions)
                : null,
            BackendAddressPools = request.Properties?.BackendAddressPools != null
                ? JsonSerializer.SerializeToElement(request.Properties.BackendAddressPools, GlobalSettings.JsonOptions)
                : null,
            LoadBalancingRules = request.Properties?.LoadBalancingRules != null
                ? JsonSerializer.SerializeToElement(request.Properties.LoadBalancingRules, GlobalSettings.JsonOptions)
                : null,
            Probes = request.Properties?.Probes != null
                ? JsonSerializer.SerializeToElement(request.Properties.Probes, GlobalSettings.JsonOptions)
                : null,
            InboundNatRules = request.Properties?.InboundNatRules != null
                ? JsonSerializer.SerializeToElement(request.Properties.InboundNatRules, GlobalSettings.JsonOptions)
                : null,
            OutboundRules = request.Properties?.OutboundRules != null
                ? JsonSerializer.SerializeToElement(request.Properties.OutboundRules, GlobalSettings.JsonOptions)
                : null
        };
    }

    [UsedImplicitly]
    public class LoadBalancerProperties
    {
        public JsonElement? FrontendIPConfigurations { get; set; }
        public JsonElement? BackendAddressPools { get; set; }
        public JsonElement? LoadBalancingRules { get; set; }
        public JsonElement? Probes { get; set; }
        public JsonElement? InboundNatRules { get; set; }
        public JsonElement? OutboundRules { get; set; }
    }
}
