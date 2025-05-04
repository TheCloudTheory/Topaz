using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

public class AzureStorageService : IServiceDefinition
{
    internal const string LocalDirectoryPath = ".azure-storage";
    private readonly ILogger logger;

    public string Name => "Azure Storage";

    public AzureStorageService(ILogger logger)
    {
        this.logger = logger;
        
        InitializeLocalStorage();
    }

    private void InitializeLocalStorage()
    {
        this.logger.LogDebug("Attempting to create Azure Storage directory...");

        if(Directory.Exists(LocalDirectoryPath) == false)
        {
            Directory.CreateDirectory(LocalDirectoryPath);
            this.logger.LogDebug("Local Azure Storage directory created.");
        }
        else
        {
            this.logger.LogDebug("Attempting to create Azure Storage directory - skipped.");
        }
    }

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new TableEndpoint(this.logger),
        new BlobEndpoint()
    ];
}
