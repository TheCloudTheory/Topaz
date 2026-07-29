using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models.Requests;

internal sealed class CreateOrUpdateAvailabilitySetRequest : IValidatable
{
    public string? Location { get; init; }
    public int? PlatformFaultDomainCount { get; init; }
    public int? PlatformUpdateDomainCount { get; init; }
    public SubResource? ProximityPlacementGroup { get; init; }
    public SubResource[]? VirtualMachines { get; init; }
    public VirtualMachineScaleSetSku? Sku { get; init; }
    public AvailabilitySetResourceProperties.ScheduledEventsPolicyData? ScheduledEventsPolicy { get; init; }
    public IDictionary<string, string>? Tags { get; init; }

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