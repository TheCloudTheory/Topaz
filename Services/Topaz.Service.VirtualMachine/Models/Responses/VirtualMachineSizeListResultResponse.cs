using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models.Responses;

internal sealed class VirtualMachineSizeListResultResponse : TopazApiModel
{
    public string? NextLink { get; set; }
    
    public VirtualMachineSize[]? Value { get; set; }
    
    public static VirtualMachineSizeListResultResponse From(VirtualMachineSize[] sizes) => new()
    {
        Value = sizes
    };
}