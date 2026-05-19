using System.Text.Json;
using Topaz.Service.VirtualMachine.Models.Requests;

namespace Topaz.Service.VirtualMachine.Models;

public sealed class VirtualMachineResourceProperties
{
    public JsonElement? HardwareProfile { get; set; }
    public JsonElement? StorageProfile { get; set; }
    public JsonElement? OsProfile { get; set; }
    public JsonElement? NetworkProfile { get; set; }
    public Guid VmId { get; set; }
    public string ProvisioningState => "Succeeded";
    public VirtualMachineInstanceView InstanceView => new();

    public static VirtualMachineResourceProperties FromRequest(CreateOrUpdateVirtualMachineRequest request)
    {
        return new VirtualMachineResourceProperties
        {
            HardwareProfile = request.Properties?.HardwareProfile,
            StorageProfile = request.Properties?.StorageProfile,
            OsProfile = request.Properties?.OsProfile,
            NetworkProfile = request.Properties?.NetworkProfile,
            VmId = Guid.NewGuid()
        };
    }

    public static void UpdateFromRequest(VirtualMachineResourceProperties properties, CreateOrUpdateVirtualMachineRequest request)
    {
        properties.HardwareProfile = request.Properties?.HardwareProfile;
        properties.StorageProfile = request.Properties?.StorageProfile;
        properties.OsProfile = request.Properties?.OsProfile;
        properties.NetworkProfile = request.Properties?.NetworkProfile;
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
