namespace Topaz.Service.ContainerRegistry.Models;

internal sealed record RegistryUsage(string Name, long Limit, long CurrentValue, string Unit);
