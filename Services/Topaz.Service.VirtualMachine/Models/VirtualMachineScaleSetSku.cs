using Topaz.ResourceManager;
using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models;

internal sealed class VirtualMachineScaleSetSku : IValidatable
{
    public int Capacity { get; init; }
    public string? Name { get; init; }
    public string? Tier { get; init; }
    
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (string.IsNullOrWhiteSpace(Tier)) return (true, null);
        if(!string.Equals(Tier, "Standard", StringComparison.OrdinalIgnoreCase) || !string.Equals(Tier, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Tier must be Standard or Basic");
        }

        return (true, null);
    }

    public static VirtualMachineScaleSetSku? From(ResourceSku? availabilitySetSku)
    {
        return new VirtualMachineScaleSetSku
        {
            Capacity = availabilitySetSku?.Capacity ?? 0,
            Name = availabilitySetSku?.Name,
            Tier = availabilitySetSku?.Tier
        };
    }
}