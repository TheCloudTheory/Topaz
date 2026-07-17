using Topaz.EventPipeline;
using Topaz.Service.Disk.Endpoints;
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

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateDiskEndpoint(_eventPipeline, _logger),
        new GetDiskEndpoint(_eventPipeline, _logger),
        new DeleteDiskEndpoint(_eventPipeline, _logger),
        new UpdateDiskEndpoint(_eventPipeline, _logger),
        new ListDisksByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListDisksBySubscriptionEndpoint(_eventPipeline, _logger),
        new GrantDiskAccessEndpoint(_eventPipeline, _logger),
        new RevokeDiskAccessEndpoint(_eventPipeline, _logger),
        new GetDiskAccessOperationStatusEndpoint(),
        new HeadDiskSasEndpoint(),
        new GetDiskSasEndpoint(),
        new PutDiskSasEndpoint()
    ];

    public void Bootstrap() { }
}
