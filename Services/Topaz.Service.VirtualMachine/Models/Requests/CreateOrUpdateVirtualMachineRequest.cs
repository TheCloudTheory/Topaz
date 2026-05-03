using System.Text.Json;
using Azure.Core;

namespace Topaz.Service.VirtualMachine.Models.Requests;

public sealed class CreateOrUpdateVirtualMachineRequest
{
    public AzureLocation? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public CreateOrUpdateVirtualMachineRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateVirtualMachineRequestProperties
    {
        public JsonElement? HardwareProfile { get; set; }
        public JsonElement? StorageProfile { get; set; }
        public JsonElement? OsProfile { get; set; }
        public JsonElement? NetworkProfile { get; set; }
    }
}
