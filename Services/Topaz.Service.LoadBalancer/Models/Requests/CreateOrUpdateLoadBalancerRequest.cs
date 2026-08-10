using System.Text.Json;
using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Shared;

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

    public static CreateOrUpdateLoadBalancerRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateLoadBalancerRequest
        {
            Location = data.Location,
            Tags = data.Tags,
            Sku = new ResourceSku
            {
                Capacity = data.Sku?.Capacity,
                Family = data.Sku?.Family,
                Name = data.Sku?.Name,
                Tier = data.Sku?.Tier
            },
            Properties =
                data.Properties.ToObjectFromJson<CreateOrUpdateLoadBalancerRequestProperties>(
                    GlobalSettings.JsonOptions)
        };
    }
}
