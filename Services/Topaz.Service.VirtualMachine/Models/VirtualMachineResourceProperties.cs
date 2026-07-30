using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.VirtualMachine.Models.Requests;

namespace Topaz.Service.VirtualMachine.Models;

public sealed class VirtualMachineResourceProperties
{
    public JsonElement? AdditionalCapabilities { get; set; }
    public JsonElement? ApplicationProfile { get; set; }
    public SubResource? AvailabilitySet { get; set; }
    public JsonElement? BillingProfile { get; set; }
    public JsonElement? CapacityReservation { get; set; }
    public JsonElement? DiagnosticsProfile { get; set; }
    public string? EvictionPolicy { get; set; }
    public string? ExtensionsTimeBudget { get; set; }
    public JsonElement? HardwareProfile { get; set; }
    public JsonElement? Host { get; set; }
    public JsonElement? HostGroup { get; set; }
    public JsonElement? InterconnectBlockProfile { get; set; }
    public string? LicenseType { get; set; }
    public JsonElement? NetworkProfile { get; set; }
    public JsonElement? OsProfile { get; set; }
    public int? PlatformFaultDomain { get; set; }
    public string? Priority { get; set; }
    public JsonElement? ProximityPlacementGroup { get; set; }
    public JsonElement? ResiliencyProfile { get; set; }
    public JsonElement? ScheduledEventsPolicy { get; set; }
    public JsonElement? ScheduledEventsProfile { get; set; }
    public JsonElement? SecurityProfile { get; set; }
    public JsonElement? StorageProfile { get; set; }
    public string? TimeCreated { get; set; }
    public string? UserData { get; set; }
    public JsonElement? VirtualMachineScaleSet { get; set; }
    public Guid VmId { get; set; }
    public string ProvisioningState => "Succeeded";
    public VirtualMachineInstanceView InstanceView => new();

    public static VirtualMachineResourceProperties FromRequest(CreateOrUpdateVirtualMachineRequest request)
    {
        return new VirtualMachineResourceProperties
        {
            AdditionalCapabilities = request.Properties?.AdditionalCapabilities,
            ApplicationProfile = request.Properties?.ApplicationProfile,
            AvailabilitySet = request.Properties?.AvailabilitySet,
            BillingProfile = request.Properties?.BillingProfile,
            CapacityReservation = request.Properties?.CapacityReservation,
            DiagnosticsProfile = request.Properties?.DiagnosticsProfile,
            EvictionPolicy = request.Properties?.EvictionPolicy,
            ExtensionsTimeBudget = request.Properties?.ExtensionsTimeBudget,
            HardwareProfile = request.Properties?.HardwareProfile,
            Host = request.Properties?.Host,
            HostGroup = request.Properties?.HostGroup,
            LicenseType = request.Properties?.LicenseType,
            NetworkProfile = request.Properties?.NetworkProfile,
            OsProfile = request.Properties?.OsProfile,
            PlatformFaultDomain = request.Properties?.PlatformFaultDomain,
            Priority = request.Properties?.Priority,
            ProximityPlacementGroup = request.Properties?.ProximityPlacementGroup,
            ScheduledEventsPolicy = request.Properties?.ScheduledEventsPolicy,
            ScheduledEventsProfile = request.Properties?.ScheduledEventsProfile,
            SecurityProfile = request.Properties?.SecurityProfile,
            StorageProfile = request.Properties?.StorageProfile,
            TimeCreated = DateTimeOffset.UtcNow.ToString("o"),
            UserData = request.Properties?.UserData,
            VirtualMachineScaleSet = request.Properties?.VirtualMachineScaleSet,
            VmId = Guid.NewGuid()
        };
    }

    public static void UpdateFromRequest(VirtualMachineResourceProperties properties, CreateOrUpdateVirtualMachineRequest request)
    {
        properties.AdditionalCapabilities = request.Properties?.AdditionalCapabilities;
        properties.ApplicationProfile = request.Properties?.ApplicationProfile;
        properties.AvailabilitySet = request.Properties?.AvailabilitySet;
        properties.BillingProfile = request.Properties?.BillingProfile;
        properties.CapacityReservation = request.Properties?.CapacityReservation;
        properties.DiagnosticsProfile = request.Properties?.DiagnosticsProfile;
        properties.EvictionPolicy = request.Properties?.EvictionPolicy;
        properties.ExtensionsTimeBudget = request.Properties?.ExtensionsTimeBudget;
        properties.HardwareProfile = request.Properties?.HardwareProfile;
        properties.Host = request.Properties?.Host;
        properties.HostGroup = request.Properties?.HostGroup;
        properties.LicenseType = request.Properties?.LicenseType;
        properties.NetworkProfile = request.Properties?.NetworkProfile;
        properties.OsProfile = request.Properties?.OsProfile;
        properties.PlatformFaultDomain = request.Properties?.PlatformFaultDomain;
        properties.Priority = request.Properties?.Priority;
        properties.ProximityPlacementGroup = request.Properties?.ProximityPlacementGroup;
        properties.ScheduledEventsPolicy = request.Properties?.ScheduledEventsPolicy;
        properties.ScheduledEventsProfile = request.Properties?.ScheduledEventsProfile;
        properties.SecurityProfile = request.Properties?.SecurityProfile;
        properties.StorageProfile = request.Properties?.StorageProfile;
        properties.UserData = request.Properties?.UserData;
        properties.VirtualMachineScaleSet = request.Properties?.VirtualMachineScaleSet;
    }

    public static void UpdateFromPatchRequest(VirtualMachineResourceProperties properties, UpdateVirtualMachineRequest request)
    {
        if (request.Properties?.AdditionalCapabilities != null)
            properties.AdditionalCapabilities = request.Properties.AdditionalCapabilities;
        if (request.Properties?.ApplicationProfile != null)
            properties.ApplicationProfile = request.Properties.ApplicationProfile;
        if (request.Properties?.BillingProfile != null)
            properties.BillingProfile = request.Properties.BillingProfile;
        if (request.Properties?.DiagnosticsProfile != null)
            properties.DiagnosticsProfile = request.Properties.DiagnosticsProfile;
        if (request.Properties?.EvictionPolicy != null)
            properties.EvictionPolicy = request.Properties.EvictionPolicy;
        if (request.Properties?.ExtensionsTimeBudget != null)
            properties.ExtensionsTimeBudget = request.Properties.ExtensionsTimeBudget;
        if (request.Properties?.HardwareProfile != null)
            properties.HardwareProfile = request.Properties.HardwareProfile;
        if (request.Properties?.LicenseType != null)
            properties.LicenseType = request.Properties.LicenseType;
        if (request.Properties?.NetworkProfile != null)
            properties.NetworkProfile = request.Properties.NetworkProfile;
        if (request.Properties?.ScheduledEventsPolicy != null)
            properties.ScheduledEventsPolicy = request.Properties.ScheduledEventsPolicy;
        if (request.Properties?.ScheduledEventsProfile != null)
            properties.ScheduledEventsProfile = request.Properties.ScheduledEventsProfile;
        if (request.Properties?.SecurityProfile != null)
            properties.SecurityProfile = request.Properties.SecurityProfile;
        if (request.Properties?.StorageProfile != null)
            properties.StorageProfile = request.Properties.StorageProfile;
        if (request.Properties?.UserData != null)
            properties.UserData = request.Properties.UserData;
    }
}

public sealed class VirtualMachineInstanceView
{
    public VirtualMachineStatus[] Statuses { get; } =
    [
        new() { Code = "ProvisioningState/succeeded", Level = "Info", DisplayStatus = "Provisioning succeeded" },
        new() { Code = "PowerState/running", Level = "Info", DisplayStatus = "VM running" }
    ];
}

public sealed class VirtualMachineStatus
{
    public string Code { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string DisplayStatus { get; init; } = string.Empty;
}
