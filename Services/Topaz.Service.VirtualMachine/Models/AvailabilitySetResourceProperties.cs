using JetBrains.Annotations;
using Topaz.Service.Shared;
using Topaz.Service.VirtualMachine.Models.Requests;

namespace Topaz.Service.VirtualMachine.Models;

internal sealed class AvailabilitySetResourceProperties
{
    public int? PlatformFaultDomainCount { get; set; }
    public int? PlatformUpdateDomainCount { get; set; }
    public SubResource? ProximityPlacementGroup { get; set; }
    public SubResource[]? VirtualMachines { get; set; }
    public VirtualMachineScaleSetSku? Sku { get; set; }
    public ScheduledEventsPolicyData? ScheduledEventsPolicy { get; set; }
    public InstanceViewStatus[]? Statuses { get; set; }
    
    public static AvailabilitySetResourceProperties From(CreateOrUpdateAvailabilitySetRequest request)
    {
        return new AvailabilitySetResourceProperties
        {
            PlatformFaultDomainCount = request.PlatformFaultDomainCount,
            PlatformUpdateDomainCount = request.PlatformUpdateDomainCount,
            ProximityPlacementGroup = request.ProximityPlacementGroup,
            VirtualMachines = request.VirtualMachines,
            Sku = request.Sku,
            ScheduledEventsPolicy = request.ScheduledEventsPolicy
        };
    }
    
    public void UpdateFromRequest(CreateOrUpdateAvailabilitySetRequest request)
    {
        PlatformFaultDomainCount = request.PlatformFaultDomainCount ?? PlatformFaultDomainCount;
        PlatformUpdateDomainCount = request.PlatformUpdateDomainCount ?? PlatformUpdateDomainCount;
        ProximityPlacementGroup = request.ProximityPlacementGroup ?? ProximityPlacementGroup;
        VirtualMachines = request.VirtualMachines ?? VirtualMachines;
        Sku = request.Sku ?? Sku;
        ScheduledEventsPolicy = request.ScheduledEventsPolicy ?? ScheduledEventsPolicy;
    }
    
    [UsedImplicitly]
    internal class InstanceViewStatus
    {
        public string? Code { get; set; }
        public string? DisplayStatus { get; set; }
        public string? Message { get; set; }
        public string? Time { get; set; }
        public string? Level { get; set; }
    }

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
}