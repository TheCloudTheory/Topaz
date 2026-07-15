using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights.Endpoints;

internal sealed class GetCurrentBillingFeaturesEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "microsoft.insights";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}/currentbillingfeatures"
    ];

    public string[] Permissions => ["microsoft.insights/components/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new CurrentBillingFeaturesResponse());
    }

    private sealed class CurrentBillingFeaturesResponse : TopazApiModel
    {
        [JsonPropertyName("CurrentBillingFeatures")]
        public string[] CurrentBillingFeatures { get; } = ["Basic"];

        [JsonPropertyName("DataVolumeCap")]
        public DataVolumeCap DataVolumeCap { get; } = new();
    }

    private sealed class DataVolumeCap
    {
        [JsonPropertyName("Cap")]
        public double Cap { get; } = 100;

        [JsonPropertyName("ResetTime")]
        public int ResetTime { get; } = 0;

        [JsonPropertyName("WarningThreshold")]
        public int WarningThreshold { get; } = 90;

        [JsonPropertyName("StopSendNotificationWhenHitThreshold")]
        public bool StopSendNotificationWhenHitThreshold { get; } = false;

        [JsonPropertyName("StopSendNotificationWhenHitCap")]
        public bool StopSendNotificationWhenHitCap { get; } = false;

        [JsonPropertyName("MaxHistoryCap")]
        public double MaxHistoryCap { get; } = 500;
    }
}
