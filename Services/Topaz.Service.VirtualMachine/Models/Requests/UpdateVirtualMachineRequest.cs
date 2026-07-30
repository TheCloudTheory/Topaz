using System.Text.Json;

namespace Topaz.Service.VirtualMachine.Models.Requests;

public sealed class UpdateVirtualMachineRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public UpdateVirtualMachineRequestProperties? Properties { get; set; }

    public sealed class UpdateVirtualMachineRequestProperties
    {
        public JsonElement? AdditionalCapabilities { get; set; }
        public JsonElement? ApplicationProfile { get; set; }
        public JsonElement? BillingProfile { get; set; }
        public JsonElement? DiagnosticsProfile { get; set; }
        public string? EvictionPolicy { get; set; }
        public string? ExtensionsTimeBudget { get; set; }
        public JsonElement? HardwareProfile { get; set; }
        public string? LicenseType { get; set; }
        public JsonElement? NetworkProfile { get; set; }
        public JsonElement? ScheduledEventsPolicy { get; set; }
        public JsonElement? ScheduledEventsProfile { get; set; }
        public JsonElement? SecurityProfile { get; set; }
        public JsonElement? StorageProfile { get; set; }
        public string? UserData { get; set; }
    }
}
