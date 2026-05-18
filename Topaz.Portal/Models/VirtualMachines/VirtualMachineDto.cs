namespace Topaz.Portal.Models.VirtualMachines;

public sealed class VirtualMachineDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? VmSize { get; init; }
    public string? ProvisioningState { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListVirtualMachinesResponse
{
    public VirtualMachineDto[] Value { get; init; } = [];
}
