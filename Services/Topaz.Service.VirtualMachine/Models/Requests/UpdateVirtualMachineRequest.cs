using System.Text.Json;

namespace Topaz.Service.VirtualMachine.Models.Requests;

public sealed class UpdateVirtualMachineRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public UpdateVirtualMachineRequestProperties? Properties { get; set; }

    public sealed class UpdateVirtualMachineRequestProperties
    {
        public JsonElement? HardwareProfile { get; set; }
        public JsonElement? StorageProfile { get; set; }
        public JsonElement? NetworkProfile { get; set; }
    }
}
