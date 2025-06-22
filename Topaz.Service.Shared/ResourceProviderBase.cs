using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public class ResourceProviderBase<TService> where TService : IServiceDefinition
{
    protected const string BaseEmulatorPath = ".topaz";
    private readonly ITopazLogger _logger;

    protected ResourceProviderBase(ITopazLogger logger)
    {
        _logger = logger;
    }

    public virtual void Delete(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        if(Directory.Exists(servicePath) == false) 
        {
            _logger.LogDebug($"The resource '{servicePath}' does not exists, no changes applied.");
            return;
        }

        _logger.LogDebug($"Deleting resource '{servicePath}'.");
        Directory.Delete(servicePath, true);
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

    public T? GetAs<T>(string id)
    {
        var raw = Get(id);
        if(string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }

    public virtual IEnumerable<string> List()
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath);
        if (Directory.Exists(servicePath) == false)
        {
            _logger.LogWarning("Trying to list resources for a non-existing service. If you see this warning, make sure you created a service (e.g subscription) before accessing its data.");
            return [];
        }
        
        var metadataFiles = Directory.EnumerateFiles(servicePath, "metadata.json", SearchOption.AllDirectories);

        return metadataFiles.Select(File.ReadAllText);
    }

    public void Create<TModel>(string id, TModel model)
    {
        var metadataFilePath = InitializeServiceDirectories(id);

        _logger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    private string InitializeServiceDirectories(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath);
        _logger.LogDebug($"Attempting to create {servicePath} directory...");

        if(Directory.Exists(servicePath) == false)
        {
            Directory.CreateDirectory(servicePath);
            _logger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            _logger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
        }
        
        const string fileName = $"metadata.json";
        var instancePath = Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, id);
        var metadataFilePath = Path.Combine(instancePath, fileName);
        var dataPath = Path.Combine(instancePath, "data");

        _logger.LogDebug($"Attempting to create {instancePath} directory.");
        if(Directory.Exists(instancePath))
        {
            _logger.LogDebug($"Attempting to create {instancePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(instancePath);
            Directory.CreateDirectory(dataPath);
            
            _logger.LogDebug($"Attempting to create {instancePath} directory - created!");
        }

        return metadataFilePath;
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
    
    public void CreateOrUpdateSubresource<TModel>(string id, string parentId, string subresource, TModel model)
    {
        if (TService.Subresources == null)
        {
            throw new  InvalidOperationException("You can't create a subresource for a parent service which defines not subresources.");    
        }
        
        if (TService.Subresources.Contains(subresource) == false)
        {
            throw new  InvalidOperationException($"You can't create a subresource '{subresource}' for a parent service which doesn't define that subresource.");  
        }
        
        var metadataFilePath = InitializeSubresourceDirectories(id, parentId, subresource);

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);
    }

    private string InitializeSubresourceDirectories(string id, string parentId, string subresource)
    {
        var metadataFile = "metadata.json";
        var subresourcePath = GetSubresourcePath(parentId, id, subresource);
        var dataPath = Path.Combine(subresourcePath, "data");
        var metadataFilePath = Path.Combine(subresourcePath, metadataFile);
        
        _logger.LogDebug($"Attempting to create {subresourcePath} directory.");
        if (Directory.Exists(subresourcePath) == false)
        {
            Directory.CreateDirectory(subresourcePath);
            _logger.LogDebug($"Attempting to create {subresourcePath} directory - created!");
        }
        else
        {
            _logger.LogDebug($"Attempting to create {subresourcePath} directory - skipped.");
        }
        
        _logger.LogDebug($"Attempting to create {metadataFilePath} file.");
        if (Directory.Exists(metadataFilePath) == false)
        {
            Directory.CreateDirectory(dataPath);
        }
        else
        {
            _logger.LogDebug($"Attempting to create {metadataFilePath} directory - skipped.");
        }
        
        return metadataFilePath;
    }

    private string? GetSubresource(string id, string parentId, string subresource)
    {
        if (TService.Subresources == null)
        {
            throw new  InvalidOperationException("You can't get a subresource for a parent service which defines not subresources.");    
        }
        
        if (TService.Subresources.Contains(subresource) == false)
        {
            throw new  InvalidOperationException($"You can't get a subresource '{subresource}' for a parent service which doesn't define that subresource.");  
        }
        
        var metadataFilePath = InitializeSubresourceDirectories(id, parentId, subresource);

        if (File.Exists(metadataFilePath) == false) return null;

        var content = File.ReadAllText(metadataFilePath);
        if(string.IsNullOrEmpty(content)) throw new InvalidOperationException("Metadata file is null or empty.");

        return content;
    }
    
    public T? GetSubresourceAs<T>(string id, string parentId, string subresource)
    {
        var raw = GetSubresource(id, parentId, subresource);
        if(string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }
    
    private string GetSubresourcePath(string parentId, string subresourceId, string subresource)
    {
        return Path.Combine(BaseEmulatorPath, TService.LocalDirectoryPath, parentId, subresource, subresourceId);
    }

    public void DeleteSubresource(string id, string parentId, string subresource)
    {
        var subresourcePath = GetSubresourcePath(parentId, id, subresource);
        if(Directory.Exists(subresourcePath) == false) 
        {
            _logger.LogDebug($"The subresource '{subresourcePath}' does not exists, no changes applied.");
            return;
        }

        _logger.LogDebug($"Deleting subresource '{subresourcePath}'.");
        Directory.Delete(subresourcePath, true);
    }
}
