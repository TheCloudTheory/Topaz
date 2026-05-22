using Topaz.Service.AppService.Models.Requests;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServicePlanResourceProperties
{
    public AppServicePlanResourceProperties() { }

    public string ProvisioningState { get; init; } = "Succeeded";
    public string Status { get; init; } = "Ready";
    public int NumberOfWorkers { get; set; } = 1;
    public int MaximumNumberOfWorkers { get; set; } = 1;
    public string? WorkerTierName { get; set; }
    public string? GeoRegion { get; set; }
    public bool HyperV { get; set; }
    public bool IsSpot { get; set; }
    public bool Reserved { get; set; }

    public static AppServicePlanResourceProperties FromRequest(CreateOrUpdateAppServicePlanRequest request)
    {
        var props = request.Properties;
        return new AppServicePlanResourceProperties
        {
            NumberOfWorkers = props?.NumberOfWorkers.GetValueOrDefault(1) ?? 1,
            MaximumNumberOfWorkers = props?.MaximumNumberOfWorkers.GetValueOrDefault(1) ?? 1,
            WorkerTierName = props?.WorkerTierName,
            HyperV = props?.HyperV.GetValueOrDefault(false) ?? false,
            IsSpot = props?.IsSpot.GetValueOrDefault(false) ?? false,
            Reserved = props?.Reserved.GetValueOrDefault(false) ?? false,
        };
    }

    public static void UpdateFromRequest(AppServicePlanResource resource, CreateOrUpdateAppServicePlanRequest request)
    {
        var props = request.Properties;
        if (props == null) return;

        if (props.NumberOfWorkers.HasValue) resource.Properties.NumberOfWorkers = props.NumberOfWorkers.Value;
        if (props.MaximumNumberOfWorkers.HasValue) resource.Properties.MaximumNumberOfWorkers = props.MaximumNumberOfWorkers.Value;
        if (props.WorkerTierName != null) resource.Properties.WorkerTierName = props.WorkerTierName;
        if (props.HyperV.HasValue) resource.Properties.HyperV = props.HyperV.Value;
        if (props.IsSpot.HasValue) resource.Properties.IsSpot = props.IsSpot.Value;
        if (props.Reserved.HasValue) resource.Properties.Reserved = props.Reserved.Value;
    }
}
