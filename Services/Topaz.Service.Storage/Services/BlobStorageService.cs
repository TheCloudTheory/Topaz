using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Blob;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public class BlobStorageService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "blobstorage";
    public string Name => "Blob Storage";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AzureStorageService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new GetBlobServiceStatsEndpoint(eventPipeline, logger),
        new ListBlobsEndpoint(eventPipeline, logger),
        new SetContainerMetadataEndpoint(eventPipeline, logger),
        new GetContainerMetadataEndpoint(eventPipeline, logger),
        new SetContainerAclEndpoint(eventPipeline, logger),
        new GetContainerAclEndpoint(eventPipeline, logger),
        new GetContainerPropertiesEndpoint(eventPipeline, logger),
        new LeaseContainerEndpoint(eventPipeline, logger),
        new CreateContainerEndpoint(eventPipeline, logger),
        new LeaseBlobEndpoint(eventPipeline, logger),
        new UndeleteBlobEndpoint(eventPipeline, logger),
        new SnapshotBlobEndpoint(eventPipeline, logger),
        new SetBlobPropertiesEndpoint(eventPipeline, logger),
        new GetBlockListEndpoint(eventPipeline, logger),
        new GetPageRangesEndpoint(eventPipeline, logger),
        new PutBlockListEndpoint(eventPipeline, logger),
        new PutBlockEndpoint(eventPipeline, logger),
        new PutPageEndpoint(eventPipeline, logger),
        new PutBlobEndpoint(eventPipeline, logger),
        new GetBlobMetadataEndpoint(eventPipeline, logger),
        new ListContainersEndpoint(eventPipeline, logger),
        new GetBlobEndpoint(eventPipeline, logger),
        new GetBlobPropertiesEndpoint(eventPipeline, logger),
        new DeleteBlobEndpoint(eventPipeline, logger),
    ];

    public void Bootstrap()
    {
    }
}
