using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;

namespace Topaz.Service.VirtualMachine;

internal sealed class AvailabilitySetService : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".availability-set");
    public static IReadOnlyCollection<string>? Subresources => [];
    public static string UniqueName => "availability-sets";
    public string Name => "Availability Sets";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
    ];
}