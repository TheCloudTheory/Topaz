using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine.Models;

internal sealed class VirtualMachineScaleSetSku : IValidatable
{
    public int Capacity { get; set; }
    public string? Name { get; set; }
    public string? Tier { get; set; }
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (string.IsNullOrWhiteSpace(Tier)) return (true, null);
        if(!string.Equals(Tier, "Standard", StringComparison.OrdinalIgnoreCase) || !string.Equals(Tier, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Tier must be Standard or Basic");
        }

        return (true, null);
    }
}