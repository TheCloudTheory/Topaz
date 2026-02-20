using System.Text.Json;
using Topaz.Dns;
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

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier,
        string? id, bool softDelete = false)
    {
        var servicePath = string.IsNullOrWhiteSpace(id) ?
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier)) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
            
        if(!Directory.Exists(servicePath)) 
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(Delete), $"The resource '{servicePath}' does not exists, no changes applied.");
            return;
        }

        // A resource wil be physically deleted only if it's not soft deleted
        if (!softDelete)
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(Delete),$"Deleting resource '{servicePath}'.");
            Directory.Delete(servicePath, true);
        }
        
        var instanceName = string.IsNullOrWhiteSpace(id)
            ? resourceGroupIdentifier == null
                ? subscriptionIdentifier.Value.ToString()
                : resourceGroupIdentifier.Value
            : id;
            
        var existingInstance = GlobalDnsEntries.GetEntry(TService.UniqueName, instanceName);
        if (existingInstance != null && TService.IsGlobalService)
        {
            GlobalDnsEntries.DeleteEntry(TService.UniqueName, subscriptionIdentifier.Value, resourceGroupIdentifier?.Value, id, softDelete);
        }
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
            return TService.LocalDirectoryPath.Replace("/{subscriptionId}", string.Empty);
        }

        if (resourceGroupIdentifier != null)
            return TService.LocalDirectoryPath.Replace("{subscriptionId}", subscriptionIdentifier.Value.ToString())
                .Replace("{resourceGroup}", resourceGroupIdentifier.Value);
        
        var path = TService.LocalDirectoryPath.Replace("{subscriptionId}", subscriptionIdentifier.Value.ToString());
        var segments = path.Split("/");
        
        return segments.Length > 3 ? string.Join("/",  segments.Take(segments.Length - 3)) : path;
    }

    public T? GetAs<T>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id = null)
    {
        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(GetAs),
            "Looking for a resource `{0}` in resource group `{1}` in subscription {2}.", id, resourceGroupIdentifier, subscriptionIdentifier);
        
        var raw = Get(subscriptionIdentifier, resourceGroupIdentifier, id);
        if(string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }

    public IEnumerable<string> List(SubscriptionIdentifier? subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id = null, uint? lookForNoOfSegments = null)
    {
        var servicePath = string.IsNullOrWhiteSpace(id) ?
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier))
            : Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
        
        if (!Directory.Exists(servicePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(List),$"Used `{servicePath}` to check if a service exists.");
            _logger.LogWarning("Trying to list resources for a non-existing service. If you see this warning, make sure you created a service (e.g subscription) before accessing its data.");
            
            return [];
        }
        
        // Add a parameter which will allow to explicitly say how many segments should be looked for.
        var metadataFiles = Directory.EnumerateFiles(servicePath, "metadata.json", SearchOption.AllDirectories);
        var servicePathSegments = TService.LocalDirectoryPath.Split("/");
        var defaultLookForNoOfSegments = servicePathSegments.Length + 2; // Local path will contain two additional segments: root directory and metadata filename 
        
        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(List),$"If lookForNoOfSegments parameter wasn't provided (was {lookForNoOfSegments}) will default to {defaultLookForNoOfSegments} segments lookup.");
        
        return metadataFiles.Where(file => file.Split("/").Length == (lookForNoOfSegments.HasValue ? lookForNoOfSegments.Value : defaultLookForNoOfSegments)).Select(File.ReadAllText);
    }

    public IEnumerable<T> ListAs<T>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id = null, uint? lookForNoOfSegments = null)
    {
        var contents = List(subscriptionIdentifier, resourceGroupIdentifier, id, lookForNoOfSegments);
        return contents.Select(file => JsonSerializer.Deserialize<T>(file, GlobalSettings.JsonOptions)!);
    }

    public void Create<TModel>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id, TModel model)
    {
        var instanceName = string.IsNullOrWhiteSpace(id)
            ? resourceGroupIdentifier == null
                ? subscriptionIdentifier.Value.ToString()
                : resourceGroupIdentifier.Value
            : id;
            
        var existingInstance = GlobalDnsEntries.GetEntry(TService.UniqueName, instanceName);
        if (existingInstance != null && TService.IsGlobalService)
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(Create),$"There's an existing instance of {TService.UniqueName} service existing with the name {instanceName}");
            return;
        }
            
        var metadataFilePath = InitializeServiceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id);

        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(Create),$"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath)) throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        if (!TService.IsGlobalService) return;
       
        GlobalDnsEntries.AddEntry(TService.UniqueName, subscriptionIdentifier.Value, resourceGroupIdentifier?.Value, instanceName);
    }

    private string InitializeServiceDirectories(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier, string? id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeServiceDirectories),$"Attempting to create {servicePath} directory...");

        if(!Directory.Exists(servicePath))
        {
            Directory.CreateDirectory(servicePath);
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeServiceDirectories),$"Directory {servicePath} created.");
        }
        else
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeServiceDirectories),$"Attempting to create {servicePath} directory - skipped.");
        }
        
        const string fileName = "metadata.json";
        var instancePath = string.IsNullOrWhiteSpace(id) ? 
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier)) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
        var metadataFilePath = Path.Combine(instancePath, fileName);
        var dataPath = Path.Combine(instancePath, "data");

        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeServiceDirectories),$"Attempting to create {instancePath} directory.");
        if(Directory.Exists(instancePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeServiceDirectories),$"Attempting to create {instancePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(instancePath);
            Directory.CreateDirectory(dataPath);
            
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeServiceDirectories),$"Attempting to create {instancePath} directory - created!");
        }

        return metadataFilePath;
    }

    public void CreateOrUpdate<TModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id, TModel model, bool createOperation = false, bool recoverInstance = false)
    {
        if(resourceGroupIdentifier == null && string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("When creating an instance both resource group and instance's ID cannot be null.");
        
        var instanceName = string.IsNullOrWhiteSpace(id)
            ? resourceGroupIdentifier!.Value
            : id;

        var existingInstance = GlobalDnsEntries.GetEntry(TService.UniqueName, instanceName);
        if (existingInstance != null && TService.IsGlobalService && createOperation && !recoverInstance)
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(CreateOrUpdate),
                $"There's an existing instance of {TService.UniqueName} service existing with the name {instanceName}");
            return;
        }

        var metadataFilePath = InitializeServiceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id);
        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);

        File.WriteAllText(metadataFilePath, content);

        if (!TService.IsGlobalService || !createOperation)
        {
            if (recoverInstance)
            {
                GlobalDnsEntries.RecoverEntry(TService.UniqueName, instanceName);
            }

            return;
        }

        GlobalDnsEntries.AddEntry(TService.UniqueName, subscriptionIdentifier.Value, resourceGroupIdentifier!.Value,
            instanceName);
    }

    public string GetServiceInstancePath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? 
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier)) :
            Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
    }
    
    public string GetServiceInstanceDataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string id)
    {
        ThrowIfIdentifierContainsForbiddenExpressions(subscriptionIdentifier.Value.ToString());
        ThrowIfIdentifierContainsForbiddenExpressions(resourceGroupIdentifier.Value);
        ThrowIfIdentifierContainsForbiddenExpressions(id);
        
        return Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id, "data");
    }

    private static void ThrowIfIdentifierContainsForbiddenExpressions(string identifier)
    {
        if (identifier.Contains("..") || identifier.Contains('/') || identifier.Contains('\\'))
        {
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        }
    }

    public void CreateOrUpdateSubresource<TModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource, TModel model)
    {
        if (TService.Subresources == null)
        {
            throw new InvalidOperationException("You can't create a subresource for a parent service which defines not subresources.");    
        }
        
        if (!TService.Subresources.Contains(subresource))
        {
            throw new InvalidOperationException($"You can't create a subresource '{subresource}' for a parent service which doesn't define that subresource.");  
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
        
        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeSubresourceDirectories),$"Attempting to create {subresourcePath} directory.");
        if (!Directory.Exists(subresourcePath))
        {
            Directory.CreateDirectory(subresourcePath);
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeSubresourceDirectories),$"Attempting to create {subresourcePath} directory - created!");
        }
        else
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeSubresourceDirectories),$"Attempting to create {subresourcePath} directory - skipped.");
        }
        
        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeSubresourceDirectories),$"Attempting to create {metadataFilePath} file.");
        if (!Directory.Exists(metadataFilePath))
        {
            Directory.CreateDirectory(dataPath);
        }
        else
        {
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(InitializeSubresourceDirectories),$"Attempting to create {metadataFilePath} directory - skipped.");
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
            _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(DeleteSubresource),$"The subresource '{subresourcePath}' does not exists, no changes applied.");
            return;
        }

        _logger.LogDebug(nameof(ResourceProviderBase<TService>), nameof(DeleteSubresource),$"Deleting subresource '{subresourcePath}'.");
        Directory.Delete(subresourcePath, true);
    }
}
