using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

public class AzureStorageService : IServiceDefinition
{
    internal const string LocalDirectoryPath = ".azure-storage";

    public string Name => "Azure Storage";

    public AzureStorageService()
    {
        InitializeLocalStorage();
    }

    private void InitializeLocalStorage()
    {
        PrettyLogger.LogDebug("Attempting to create Azure Storage directory...");

        if(Directory.Exists(LocalDirectoryPath) == false)
        {
            Directory.CreateDirectory(LocalDirectoryPath);
            PrettyLogger.LogDebug("Local Azure Storage directory created.");
        }
        else
        {
            PrettyLogger.LogDebug("Attempting to create Azure Storage directory - skipped.");
        }
    }

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new TableEndpoint(),
        new BlobEndpoint()
    ];
}
