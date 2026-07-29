using JetBrains.Annotations;

namespace Topaz.Service.VirtualMachine.Models;

[UsedImplicitly]
internal sealed class VirtualMachineSize
{
    public int? MaxDataDiskCount { get; set; }
    public int? MemoryInMB { get; set; }
    public int? NumberOfCores { get; set; }
    public string? Name { get; set; }
    public int? ResourceDiskSizeInMB { get; set; }
    public int? OSDiskSizeInMB { get; set; }
}