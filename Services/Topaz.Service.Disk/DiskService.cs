using Topaz.EventPipeline;
using Topaz.Service.Disk.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Disk;

public sealed class DiskService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".managed-disk");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "disk";

    public string Name => "Azure Managed Disks";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateDiskEndpoint(eventPipeline, logger),
        new GetDiskEndpoint(eventPipeline, logger),
        new DeleteDiskEndpoint(eventPipeline, logger),
        new UpdateDiskEndpoint(eventPipeline, logger),
        new ListDisksByResourceGroupEndpoint(eventPipeline, logger),
        new ListDisksBySubscriptionEndpoint(eventPipeline, logger),
        new GrantDiskAccessEndpoint(eventPipeline, logger),
        new RevokeDiskAccessEndpoint(eventPipeline, logger),
        new GetDiskAccessOperationStatusEndpoint(),
        new HeadDiskSasEndpoint(),
        new GetDiskSasEndpoint(),
        new PutDiskSasEndpoint()
    ];
}
