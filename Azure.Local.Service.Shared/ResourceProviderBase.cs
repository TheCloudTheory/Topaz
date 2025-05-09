using Azure.Local.Shared;

namespace Azure.Local.Service.Shared;

public abstract class ResourceProviderBase<TService> where TService : IServiceDefinition
{
    private const string BaseEmulatorPath = ".abazure";
    private readonly ILogger logger;
    
    protected ResourceProviderBase(ILogger logger)
    {
        this.logger = logger;

        InitializeServiceDirectory();
    }

    private void InitializeServiceDirectory()
    {
        this.logger.LogDebug("Attempting to create Azure Key Vault directory...");

        if(Directory.Exists(TService.LocalDirectoryPath) == false)
        {
            Directory.CreateDirectory(TService.LocalDirectoryPath);
            this.logger.LogDebug("Local rAzure Key Vault directory created.");
        }
        else
        {
            this.logger.LogDebug("Attempting to create Azure Key Vault directory - skipped.");
        }
    }

    public void Delete(string name)
    {
        var fileName = $"{name}.json";
        var resourceGroupPath = Path.Combine(TService.LocalDirectoryPath, fileName);
        if(File.Exists(resourceGroupPath) == false) 
        {
            this.logger.LogDebug($"The resource '{name}' does not exists, no changes applied.");
            return;
        }

        this.logger.LogDebug($"Deleting resource '{name}'.");
        File.Delete(resourceGroupPath);

        return;
    }

    public string Get(string id)
    {
        var fileName = $"metadata.json";
        var metadataFile = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id, fileName);

        if(File.Exists(metadataFile) == false) throw new FileNotFoundException($"Metadata file for {typeof(TService)} with ID {id} doesn't exist.");

        var content = File.ReadAllText(metadataFile);
        if(string.IsNullOrEmpty(content)) throw new InvalidOperationException("Metadata file is null or empty.");

        return content;
    }
}
