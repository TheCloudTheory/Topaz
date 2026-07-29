using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models.Responses;

internal sealed class AvailabilitySetListResultResponse : TopazApiModel
{
    public string? NextLink { get; set; }
    
    public AvailabilitySetResource[]? Value { get; set; }
    
    public static AvailabilitySetListResultResponse From(AvailabilitySetResource[] availabilitySets) => new()
    {
        Value = availabilitySets
    };
}