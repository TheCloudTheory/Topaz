using JetBrains.Annotations;

namespace Topaz.Service.EventHub.Models.Requests;

public class CreateOrUpdateEventHubNamespaceRequest
{
    public string? Location { get; init; }
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
}