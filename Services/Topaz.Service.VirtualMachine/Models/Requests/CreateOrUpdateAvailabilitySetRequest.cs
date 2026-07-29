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
    public ScheduledEventsPolicyData? ScheduledEventsPolicy { get; set; }
    
    [UsedImplicitly]
    internal class ScheduledEventsPolicyData
    {
        public AllInstancesDownData? AllInstancesDown { get; set; }
        public ScheduledEventsAdditionalPublishingTargetsData? ScheduledEventsAdditionalPublishingTargets { get; set; }
        public UserInitiatedRebootData? UserInitiatedReboot { get; set; }
        public UserInitiatedRedeployData? UserInitiatedRedeploy { get; set; }
    }

    [UsedImplicitly]
    internal class AllInstancesDownData
    {
        public bool? AutomaticallyApprove { get; set; }
    }

    [UsedImplicitly]
    internal class ScheduledEventsAdditionalPublishingTargetsData
    {
        public EventGridAndResourceGraphData? EventGridAndResourceGraph { get; set; }
    }

    [UsedImplicitly]
    internal class EventGridAndResourceGraphData
    {
        public bool? Enable { get; set; }
        public string? ScheduledEventsApiVersion { get; set; }
    }

    [UsedImplicitly]
    internal class UserInitiatedRebootData
    {
        public bool? AutomaticallyApprove { get; set; }
    }

    [UsedImplicitly]
    internal class UserInitiatedRedeployData
    {
        public bool? AutomaticallyApprove { get; set; }
    }

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