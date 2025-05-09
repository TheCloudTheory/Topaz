using System.Text.Json;
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
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath);
        this.logger.LogDebug($"Attempting to create {servicePath} directory...");

        if(Directory.Exists(servicePath) == false)
        {
            Directory.CreateDirectory(servicePath);
            this.logger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            this.logger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
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

    public void Create<TModel>(string id, TModel model)
    {
        var fileName = $"metadata.json";
        var instancePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        var metadataFilePath = Path.Combine(instancePath, fileName);

        this.logger.LogDebug($"Attempting to create {instancePath} directory.");
        if(Directory.Exists(instancePath))
        {
            this.logger.LogDebug($"Attempting to create {instancePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(instancePath);
            this.logger.LogDebug($"Attempting to create {instancePath} directory - created!");
        }

        this.logger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }
}
