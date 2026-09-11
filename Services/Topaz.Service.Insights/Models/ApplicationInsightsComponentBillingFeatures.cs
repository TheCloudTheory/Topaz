using System.Text.Json.Serialization;
using Topaz.Service.Shared;

namespace Topaz.Service.Insights.Models;

public sealed class ApplicationInsightsComponentBillingFeatures : TopazApiModel
{
    private const int BasicPlanRetentionInDays = 30;

    private const string BasicPlan = "Basic";
    private const string EnterprisePlan = "Application Insights Enterprise";

    [JsonPropertyName("CurrentBillingFeatures")]
    public string[] CurrentBillingFeatures { get; set; } = [BasicPlan];

    [JsonPropertyName("DataVolumeCap")]
    public ApplicationInsightsComponentDataVolumeCap DataVolumeCap { get; set; } = new();

    public static string[] GetPlanForRetention(int retentionInDays) =>
        retentionInDays > BasicPlanRetentionInDays ? [BasicPlan, EnterprisePlan] : [BasicPlan];
}

public sealed class ApplicationInsightsComponentDataVolumeCap
{
    [JsonPropertyName("Cap")]
    public double Cap { get; set; } = 100;

    [JsonPropertyName("ResetTime")]
    public int ResetTime { get; set; }

    [JsonPropertyName("WarningThreshold")]
    public int WarningThreshold { get; set; } = 90;

    [JsonPropertyName("StopSendNotificationWhenHitThreshold")]
    public bool StopSendNotificationWhenHitThreshold { get; set; }

    [JsonPropertyName("StopSendNotificationWhenHitCap")]
    public bool StopSendNotificationWhenHitCap { get; set; }

    [JsonPropertyName("MaxHistoryCap")]
    public double MaxHistoryCap { get; set; } = 500;
}
