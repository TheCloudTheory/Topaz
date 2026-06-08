using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Disk;

public sealed class DiskService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".managed-disk");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "disk";

    public string Name => "Azure Managed Disks";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];

    public void Bootstrap() { }
}
