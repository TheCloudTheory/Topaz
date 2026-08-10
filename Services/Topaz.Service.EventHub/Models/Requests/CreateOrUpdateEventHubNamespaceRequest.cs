using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Models.Requests;

public class CreateOrUpdateEventHubNamespaceRequest
{
    public string? Location { get; init; }
    public ResourceSku? Sku { get; init; }
    public CreateOrUpdateEventHubNamespaceRequestProperties?  Properties { get; init; }

    [UsedImplicitly]
    public class CreateOrUpdateEventHubNamespaceRequestProperties
    {
        public bool? DisableLocalAuth { get; set; } = false;
        public bool? IsAutoInflateEnabled { get; set; } = false;
        public bool? KafkaEnabled { get; set; } = false;
        public int? MaximumThroughputUnits { get; set; }
        public string? MinimumTlsVersion { get; set; } = "1.2";
        public string? PublicNetworkAccess { get; set; } = "Enabled";
        public bool? ZoneRedundant { get; set; } = false;
    }

    public static CreateOrUpdateEventHubNamespaceRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateEventHubNamespaceRequest
        {
            Location = data.Location,
            Sku = new ResourceSku
            {
                Capacity = data.Sku?.Capacity,
                Name = data.Sku?.Name,
                Tier = data.Sku?.Tier,
                Family = data.Sku?.Family
            },
            Properties =
                data.Properties.ToObjectFromJson<CreateOrUpdateEventHubNamespaceRequestProperties>(GlobalSettings
                    .JsonOptions)
        };
    }
}