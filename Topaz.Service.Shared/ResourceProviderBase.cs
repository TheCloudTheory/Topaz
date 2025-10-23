using System.Text.Json;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public class ResourceProviderBase<TService> where TService : IServiceDefinition
{
    protected const string BaseEmulatorPath = GlobalSettings.MainEmulatorDirectory;
    private readonly ITopazLogger _logger;

    protected ResourceProviderBase(ITopazLogger logger)
    {
        _logger = logger;
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id)
    {
        var servicePath = string.IsNullOrWhiteSpace(id) ?
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier)) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
            
        if(!Directory.Exists(servicePath)) 
        {
            _logger.LogDebug($"The resource '{servicePath}' does not exists, no changes applied.");
            return;
        }

        _logger.LogDebug($"Deleting resource '{servicePath}'.");
        Directory.Delete(servicePath, true);
    }

    public string? Get(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id)
    {
        const string fileName = $"metadata.json";
        var metadataFile = string.IsNullOrWhiteSpace(id) ? 
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), fileName) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id, fileName);

        if (!File.Exists(metadataFile)) return null;

        var content = File.ReadAllText(metadataFile);
        return string.IsNullOrEmpty(content) ? throw new InvalidOperationException("Metadata file is null or empty.") : content;
    }

    private static string GetLocalDirectoryPathWithReplacedValues(SubscriptionIdentifier? subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier)
    {
        if (subscriptionIdentifier == null)
        {
            return TService.LocalDirectoryPath;
        }
        
        if (resourceGroupIdentifier == null)
        {
            return TService.LocalDirectoryPath.Replace("{subscriptionId}", subscriptionIdentifier.Value.ToString());
        }
        
        return TService.LocalDirectoryPath.Replace("{subscriptionId}", subscriptionIdentifier.Value.ToString())
            .Replace("{resourceGroup}", resourceGroupIdentifier.Value);
    }

    public T? GetAs<T>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string? id = null)
    {
        var raw = Get(subscriptionIdentifier, resourceGroupIdentifier, id);
        if(string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }

    public IEnumerable<string> List(SubscriptionIdentifier? subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        if (!Directory.Exists(servicePath))
        {
            _logger.LogWarning("Trying to list resources for a non-existing service. If you see this warning, make sure you created a service (e.g subscription) before accessing its data.");
            return [];
        }
        
        var metadataFiles = Directory.EnumerateFiles(servicePath, "metadata.json", SearchOption.AllDirectories);

        return metadataFiles.Select(File.ReadAllText);
    }

    public IEnumerable<T?> ListAs<T>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier)
    {
        var contents = List(subscriptionIdentifier, resourceGroupIdentifier);
        return contents.Select(file => JsonSerializer.Deserialize<T>(file, GlobalSettings.JsonOptions));
    }

    public void Create<TModel>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id, TModel model)
    {
        var metadataFilePath = InitializeServiceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id);

        _logger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath)) throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        if (TService.IsGlobalService)
        {
            
        }
    }

    private string InitializeServiceDirectories(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        _logger.LogDebug($"Attempting to create {servicePath} directory...");

        if(!Directory.Exists(servicePath))
        {
            Directory.CreateDirectory(servicePath);
            _logger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            _logger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
        }
        
        const string fileName = "metadata.json";
        var instancePath = string.IsNullOrWhiteSpace(id) ? 
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier)) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
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

    public void CreateOrUpdate<TModel>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string? id, TModel model)
    {
        var metadataFilePath = InitializeServiceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id);
        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);

        File.WriteAllText(metadataFilePath, content);
    }

    public string GetServiceInstancePath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? 
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier)) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
    }
    
    public void CreateOrUpdateSubresource<TModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource, TModel model)
    {
        if (TService.Subresources == null)
        {
            throw new  InvalidOperationException("You can't create a subresource for a parent service which defines not subresources.");    
        }
        
        if (!TService.Subresources.Contains(subresource))
        {
            throw new  InvalidOperationException($"You can't create a subresource '{subresource}' for a parent service which doesn't define that subresource.");  
        }
        
        var metadataFilePath = InitializeSubresourceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id, parentId, subresource);

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);
    }

    private string InitializeSubresourceDirectories(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        const string metadataFile = "metadata.json";
        var subresourcePath = GetSubresourcePath(subscriptionIdentifier, resourceGroupIdentifier, parentId, id, subresource);
        var dataPath = Path.Combine(subresourcePath, "data");
        var metadataFilePath = Path.Combine(subresourcePath, metadataFile);
        
        _logger.LogDebug($"Attempting to create {subresourcePath} directory.");
        if (!Directory.Exists(subresourcePath))
        {
            Directory.CreateDirectory(subresourcePath);
            _logger.LogDebug($"Attempting to create {subresourcePath} directory - created!");
        }
        else
        {
            _logger.LogDebug($"Attempting to create {subresourcePath} directory - skipped.");
        }
        
        _logger.LogDebug($"Attempting to create {metadataFilePath} file.");
        if (!Directory.Exists(metadataFilePath))
        {
            Directory.CreateDirectory(dataPath);
        }
        else
        {
            _logger.LogDebug($"Attempting to create {metadataFilePath} directory - skipped.");
        }
        
        return metadataFilePath;
    }

    private string? GetSubresource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        if (TService.Subresources == null)
        {
            throw new InvalidOperationException("You can't get a subresource for a parent service which defines not subresources.");    
        }
        
        if (!TService.Subresources.Contains(subresource))
        {
            throw new  InvalidOperationException($"You can't get a subresource '{subresource}' for a parent service which doesn't define that subresource.");  
        }
        
        var metadataFilePath = InitializeSubresourceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id, parentId, subresource);

        if (!File.Exists(metadataFilePath)) return null;

        var content = File.ReadAllText(metadataFilePath);
        return string.IsNullOrEmpty(content) ? throw new InvalidOperationException("Metadata file is null or empty.") : content;
    }
    
    public T? GetSubresourceAs<T>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        var raw = GetSubresource(subscriptionIdentifier, resourceGroupIdentifier, id, parentId, subresource);
        if(string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }

    private string GetSubresourcePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string parentId, string subresourceId, string subresource)
    {
        return Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), parentId,
            subresource, subresourceId);
    }

    public void DeleteSubresource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        var subresourcePath = GetSubresourcePath(subscriptionIdentifier, resourceGroupIdentifier, parentId, id, subresource);
        if(!Directory.Exists(subresourcePath)) 
        {
            _logger.LogDebug($"The subresource '{subresourcePath}' does not exists, no changes applied.");
            return;
        }

        _logger.LogDebug($"Deleting subresource '{subresourcePath}'.");
        Directory.Delete(subresourcePath, true);
    }
}
