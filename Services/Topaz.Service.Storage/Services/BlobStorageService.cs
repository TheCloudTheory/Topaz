using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints;
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
        new BlobEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}