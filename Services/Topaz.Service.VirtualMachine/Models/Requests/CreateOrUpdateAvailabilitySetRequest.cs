using JetBrains.Annotations;
using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models.Requests;

internal sealed class CreateOrUpdateAvailabilitySetRequest : IValidatable
{
    public string? Location { get; set; }
    public int? PlatformFaultDomainCount { get; set; }
    public int? PlatformUpdateDomainCount { get; set; }
    public SubResource? ProximityPlacementGroup { get; set; }
    public SubResource[]? VirtualMachines { get; set; }
    public VirtualMachineScaleSetSku? Sku { get; set; }
    public AvailabilitySetResourceProperties.ScheduledEventsPolicyData? ScheduledEventsPolicy { get; set; }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (string.IsNullOrWhiteSpace(Location))
        {
            return (false, "Location is required");
        }

        if (Sku == null) return (true, null);
        var skuValidationResult = Sku.Validate<VirtualMachineScaleSetSku>();
        return !skuValidationResult.IsValid ? (false, skuValidationResult.Error) : (true, null);
    }
}