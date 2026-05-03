using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

public sealed class VirtualMachineService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;

    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".virtual-machine");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "virtual-machine";

    public string Name => "Azure Virtual Machines";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];

    public void Bootstrap()
    {
        _ = _eventPipeline;
        _ = _logger;
    }
}
