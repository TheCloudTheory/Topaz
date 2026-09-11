namespace Topaz.Service.Insights.Models;

internal sealed class UpdateBillingFeaturesRequest
{
    public UpdateDataVolumeCapRequest? DataVolumeCap { get; set; }
}

internal sealed class UpdateDataVolumeCapRequest
{
    public double? Cap { get; set; }
    public int? ResetTime { get; set; }
    public int? WarningThreshold { get; set; }
    public bool? StopSendNotificationWhenHitThreshold { get; set; }
    public bool? StopSendNotificationWhenHitCap { get; set; }
    public double? MaxHistoryCap { get; set; }
}
