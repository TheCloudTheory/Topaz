using System.Net;
using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public abstract class ResourceProviderBase<TService> where TService : IServiceDefinition
{
    protected const string BaseEmulatorPath = ".abazure";
    private readonly ILogger logger;
    
    protected ResourceProviderBase(ILogger logger)
    {
        this.logger = logger;

        InitializeServiceDirectory();
    }

    protected virtual void InitializeServiceDirectory()
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

    public virtual void Delete(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        if(Directory.Exists(servicePath) == false) 
        {
            this.logger.LogDebug($"The resource '{servicePath}' does not exists, no changes applied.");
            return;
        }

        this.logger.LogDebug($"Deleting resource '{servicePath}'.");
        Directory.Delete(servicePath, true);

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

    public virtual IEnumerable<string> List(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath);
        return Directory.EnumerateFiles(servicePath);
    }

    public virtual void Create<TModel>(string id, TModel model)
    {
        var fileName = $"metadata.json";
        var instancePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        var metadataFilePath = Path.Combine(instancePath, fileName);
        var dataPath = Path.Combine(instancePath, "data");

        this.logger.LogDebug($"Attempting to create {instancePath} directory.");
        if(Directory.Exists(instancePath))
        {
            this.logger.LogDebug($"Attempting to create {instancePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(instancePath);
            Directory.CreateDirectory(dataPath);
            
            this.logger.LogDebug($"Attempting to create {instancePath} directory - created!");
        }

        this.logger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    public (TModel data, HttpStatusCode code) CreateOrUpdate<TModel, TRequest>(string id, Stream input, Func<TRequest, TModel> requestMapper)
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

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var request = JsonSerializer.Deserialize<TRequest>(rawContent, GlobalSettings.JsonOptions);
        var newData = requestMapper(request!);
        var content = JsonSerializer.Serialize(newData, GlobalSettings.JsonOptions);

        if(File.Exists(metadataFilePath))
        {
            this.logger.LogDebug($"Attempting to create {metadataFilePath} file - file exists, it will be overwritten.");
            File.WriteAllText(metadataFilePath, content);

            return (data: newData, code: HttpStatusCode.OK);
        }

        File.WriteAllText(metadataFilePath, content);

        return (data: newData, code: HttpStatusCode.Created);
    }

    public string GetServiceInstancePath(string id)
    {
        return Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
    }
}
