using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Blob;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public class BlobStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "blobstorage";
    public string Name => "Blob Storage";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AzureStorageService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ListBlobsEndpoint(logger),
        new SetContainerMetadataEndpoint(logger),
        new GetContainerMetadataEndpoint(logger),
        new SetContainerAclEndpoint(logger),
        new GetContainerAclEndpoint(logger),
        new GetContainerPropertiesEndpoint(logger),
        new LeaseContainerEndpoint(logger),
        new CreateContainerEndpoint(logger),
        new SetBlobPropertiesEndpoint(logger),
        new GetBlockListEndpoint(logger),
        new GetPageRangesEndpoint(logger),
        new PutBlockListEndpoint(logger),
        new PutBlockEndpoint(logger),
        new PutPageEndpoint(logger),
        new PutBlobEndpoint(logger),
        new GetBlobMetadataEndpoint(logger),
        new ListContainersEndpoint(logger),
        new GetBlobEndpoint(logger),
        new GetBlobPropertiesEndpoint(logger),
        new DeleteBlobEndpoint(logger),
    ];

    public void Bootstrap()
    {
    }
}
