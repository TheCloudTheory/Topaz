using System.Text.Json;
using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models.Requests;

public sealed class CreateOrUpdateVirtualMachineRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public CreateOrUpdateVirtualMachineRequestProperties? Properties { get; init; }

    public sealed class CreateOrUpdateVirtualMachineRequestProperties
    {
        public JsonElement? AdditionalCapabilities { get; init; }
        public JsonElement? ApplicationProfile { get; init; }
        public SubResource? AvailabilitySet { get; init; }
        public JsonElement? BillingProfile { get; init; }
        public JsonElement? CapacityReservation { get; init; }
        public JsonElement? DiagnosticsProfile { get; init; }
        public string? EvictionPolicy { get; init; }
        public string? ExtensionsTimeBudget { get; init; }
        public JsonElement? HardwareProfile { get; init; }
        public JsonElement? Host { get; init; }
        public JsonElement? HostGroup { get; init; }
        public string? LicenseType { get; init; }
        public JsonElement? NetworkProfile { get; init; }
        public JsonElement? OsProfile { get; init; }
        public int? PlatformFaultDomain { get; init; }
        public string? Priority { get; init; }
        public JsonElement? ProximityPlacementGroup { get; init; }
        public JsonElement? ScheduledEventsPolicy { get; init; }
        public JsonElement? ScheduledEventsProfile { get; init; }
        public JsonElement? SecurityProfile { get; init; }
        public JsonElement? StorageProfile { get; init; }
        public string? UserData { get; init; }
        public JsonElement? VirtualMachineScaleSet { get; init; }

        public static CreateOrUpdateVirtualMachineRequestProperties From(VirtualMachineResourceProperties properties)
        {
            return new CreateOrUpdateVirtualMachineRequestProperties
            {
                AdditionalCapabilities = properties.AdditionalCapabilities,
                AvailabilitySet = properties.AvailabilitySet,
                StorageProfile = properties.StorageProfile,
                OsProfile = properties.OsProfile,
                NetworkProfile = properties.NetworkProfile,
                ApplicationProfile = properties.ApplicationProfile,
                BillingProfile = properties.BillingProfile,
                CapacityReservation = properties.CapacityReservation,
                SecurityProfile = properties.SecurityProfile,
                DiagnosticsProfile = properties.DiagnosticsProfile,
                EvictionPolicy = properties.EvictionPolicy,
                VirtualMachineScaleSet = properties.VirtualMachineScaleSet,
                ScheduledEventsPolicy = properties.ScheduledEventsPolicy,
                ScheduledEventsProfile = properties.ScheduledEventsProfile,
                ProximityPlacementGroup = properties.ProximityPlacementGroup,
                PlatformFaultDomain = properties.PlatformFaultDomain,
                Priority = properties.Priority,
                LicenseType = properties.LicenseType,
                Host = properties.Host,
                HardwareProfile = properties.HardwareProfile,
                HostGroup = properties.HostGroup,
                ExtensionsTimeBudget = properties.ExtensionsTimeBudget,
                UserData = properties.UserData
            };
        }
    }
}
