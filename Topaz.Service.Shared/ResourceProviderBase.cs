using System.Net;
using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public class ResourceProviderBase<TService> where TService : IServiceDefinition
{
    protected const string BaseEmulatorPath = ".topaz";
    private readonly ITopazLogger _topazLogger;

    protected ResourceProviderBase(ITopazLogger logger)
    {
        _topazLogger = logger;
    }

    public virtual void Delete(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        if(Directory.Exists(servicePath) == false) 
        {
            _topazLogger.LogDebug($"The resource '{servicePath}' does not exists, no changes applied.");
            return;
        }

        _topazLogger.LogDebug($"Deleting resource '{servicePath}'.");
        Directory.Delete(servicePath, true);

        return;
    }

    public string? Get(string id)
    {
        const string fileName = $"metadata.json";
        var metadataFile = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id, fileName);

        if (File.Exists(metadataFile) == false) return null;

        var content = File.ReadAllText(metadataFile);
        if(string.IsNullOrEmpty(content)) throw new InvalidOperationException("Metadata file is null or empty.");

        return content;
    }

    public virtual IEnumerable<string> List(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath);
        return Directory.EnumerateFiles(servicePath);
    }

    public void Create<TModel>(string id, TModel model)
    {
        var metadataFilePath = InitializeServiceDirectories(id);

        _topazLogger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    private string InitializeServiceDirectories(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath);
        _topazLogger.LogDebug($"Attempting to create {servicePath} directory...");

        if(Directory.Exists(servicePath) == false)
        {
            Directory.CreateDirectory(servicePath);
            _topazLogger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            _topazLogger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
        }
        
        const string fileName = $"metadata.json";
        var instancePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        var metadataFilePath = Path.Combine(instancePath, fileName);
        var dataPath = Path.Combine(instancePath, "data");

        _topazLogger.LogDebug($"Attempting to create {instancePath} directory.");
        if(Directory.Exists(instancePath))
        {
            _topazLogger.LogDebug($"Attempting to create {instancePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(instancePath);
            Directory.CreateDirectory(dataPath);
            
            _topazLogger.LogDebug($"Attempting to create {instancePath} directory - created!");
        }

        return metadataFilePath;
    }

    public (TModel data, HttpStatusCode code) CreateOrUpdate<TModel, TRequest>(string id, Stream input, Func<TRequest, TModel> requestMapper)
    {
        var metadataFilePath = InitializeServiceDirectories(id);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var request = JsonSerializer.Deserialize<TRequest>(rawContent, GlobalSettings.JsonOptions);
        var newData = requestMapper(request!);
        var content = JsonSerializer.Serialize(newData, GlobalSettings.JsonOptions);

        if(File.Exists(metadataFilePath))
        {
            _topazLogger.LogDebug($"Attempting to create {metadataFilePath} file - file exists, it will be overwritten.");
            File.WriteAllText(metadataFilePath, content);

            return (data: newData, code: HttpStatusCode.OK);
        }

        File.WriteAllText(metadataFilePath, content);

        return (data: newData, code: HttpStatusCode.Created);
    }

    public void CreateOrUpdate<TModel>(string id, TModel model)
    {
        var metadataFilePath = InitializeServiceDirectories(id);
        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);

        File.WriteAllText(metadataFilePath, content);
    }

    public string GetServiceInstancePath(string id)
    {
        return Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
    }
}
