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
        var basePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        var servicePath = string.IsNullOrWhiteSpace(id)
            ? basePath
            : Path.Combine(basePath, ResolveIdCasing(basePath, id));
        if (!Directory.Exists(servicePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(Delete),
                $"The resource '{servicePath}' does not exists, no changes applied.");
            return;
        }

        // A resource will be physically deleted only if it's not soft deleted
        if (!softDelete)
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(Delete),
                $"Deleting resource '{servicePath}'.");
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
            GlobalDnsEntries.DeleteEntry(TService.UniqueName, subscriptionIdentifier.Value,
                resourceGroupIdentifier?.Value, id, softDelete);
        }
    }

    public string? Get(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier,
        string? id)
    {
        const string fileName = $"metadata.json";
        var basePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        var resolvedId = string.IsNullOrWhiteSpace(id) ? null : ResolveIdCasing(basePath, id);
        var metadataFile = resolvedId == null
            ? Path.Combine(basePath, fileName)
            : Path.Combine(basePath, resolvedId, fileName);

        if (!File.Exists(metadataFile)) return null;

        var content = File.ReadAllText(metadataFile);
        return string.IsNullOrEmpty(content)
            ? throw new InvalidOperationException("Metadata file is null or empty.")
            : content;
    }

    /// <summary>
    /// Resolves the on-disk casing of a resource directory name under <paramref name="parentPath"/>.
    /// On case-sensitive filesystems (Linux), a caller may supply a name that differs in casing from
    /// the directory that was originally created (e.g. the Azure CLI lowercases hostnames). This method
    /// performs a case-insensitive directory search so that reads and writes always target the correct path
    /// regardless of the casing supplied by the caller.
    /// </summary>
    /// <param name="parentPath">The parent directory under which the resource directory lives.</param>
    /// <param name="id">The resource identifier / directory name to resolve.</param>
    /// <returns>
    /// The directory name with its original on-disk casing if a match is found; otherwise <paramref name="id"/>
    /// unchanged (allowing callers to handle the not-found case normally).
    /// </returns>
    private static string ResolveIdCasing(string parentPath, string id)
    {
        if (Directory.Exists(Path.Combine(parentPath, id))) return id;
        if (!Directory.Exists(parentPath)) return id;

        var match = Directory.EnumerateDirectories(parentPath)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), id, StringComparison.OrdinalIgnoreCase));
        return match != null ? Path.GetFileName(match) : id;
    }

    protected static string GetLocalDirectoryPathWithReplacedValues(SubscriptionIdentifier? subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier)
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

        return segments.Length > 3 ? string.Join("/", segments.Take(segments.Length - 3)) : path;
    }

    public T? GetAs<T>(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier? resourceGroupIdentifier,
        string? id = null)
    {
        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(GetAs),
            "Looking for a resource `{0}` in resource group `{1}` in subscription {2}.", id, resourceGroupIdentifier,
            subscriptionIdentifier);

        var raw = Get(subscriptionIdentifier, resourceGroupIdentifier, id);
        if (string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }

    public IEnumerable<string> List(SubscriptionIdentifier? subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id = null, uint? lookForNoOfSegments = null,
        string? filter = null)
    {
        var listBasePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        var servicePath = string.IsNullOrWhiteSpace(id)
            ? listBasePath
            : Path.Combine(listBasePath, ResolveIdCasing(listBasePath, id));
        
        if (!Directory.Exists(servicePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(List),
                $"Path `{servicePath}` does not exist — returning empty list. Note that this indicates a service has not been initialized ");

            return [];
        }

        // Add a parameter which will allow explicitly saying how many segments should be looked for.
        var metadataFiles = Directory.EnumerateFiles(servicePath, "metadata.json", SearchOption.AllDirectories);
        var servicePathSegments = TService.LocalDirectoryPath.Split("/");
        var defaultLookForNoOfSegments =
            servicePathSegments.Length +
            2; // Local path will contain two additional segments: root directory and metadata filename 

        // Derive a service-specific directory discriminator (e.g. ".managed-identity") from the local
        // directory path template. This is the last path segment that is not a {placeholder}. It prevents
        // files from other services that happen to sit at the same depth from being included in results.
        var sep = Path.DirectorySeparatorChar.ToString();
        var serviceDiscriminator = TService.LocalDirectoryPath
            .Split(['/', Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(s => !s.StartsWith('{'));

        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(List),
            $"If lookForNoOfSegments parameter wasn't provided (was {lookForNoOfSegments}) will default to {defaultLookForNoOfSegments} segments lookup. Service discriminator: '{serviceDiscriminator}'.");

        return metadataFiles
            .Where(file =>
                file.Split("/").Length ==
                (lookForNoOfSegments.HasValue ? lookForNoOfSegments.Value : defaultLookForNoOfSegments)
                && (filter == null || file.Contains(filter))
                && (serviceDiscriminator == null || file.Contains(sep + serviceDiscriminator + sep)))
            .Select(File.ReadAllText);
    }

    public IEnumerable<T> ListAs<T>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id = null, uint? lookForNoOfSegments = null,
        Func<string, bool>? filter = null)
    {
        var contents = List(subscriptionIdentifier, resourceGroupIdentifier, id, lookForNoOfSegments);
        if (filter != null)
        {
            contents = contents.Where(filter);    
        }
        
        return contents.Select(file => JsonSerializer.Deserialize<T>(file, GlobalSettings.JsonOptions)!);
    }

    public void Create<TModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id, TModel model)
    {
        var instanceName = string.IsNullOrWhiteSpace(id)
            ? resourceGroupIdentifier == null
                ? subscriptionIdentifier.Value.ToString()
                : resourceGroupIdentifier.Value
            : id;

        var existingInstance = GlobalDnsEntries.GetEntry(TService.UniqueName, instanceName);
        if (existingInstance != null && TService.IsGlobalService)
        {
            // Verify the resource the DNS entry points to still exists on disk.
            // If the metadata file is gone, the entry is an orphan (e.g. the directory was deleted
            // without a corresponding DNS cleanup), and we can safely remove it and proceed.
            // If the file IS present, the resource genuinely exists, so we return early.
            var existingSubId = SubscriptionIdentifier.From(existingInstance.Value.subscription);
            var existingRgId = existingInstance.Value.resourceGroup != null
                ? ResourceGroupIdentifier.From(existingInstance.Value.resourceGroup)
                : null;
            var existingPath = existingRgId != null
                ? GetServiceInstancePath(existingSubId, existingRgId, existingInstance.Value.canonicalName)
                : Path.Combine(BaseEmulatorPath,
                    GetLocalDirectoryPathWithReplacedValues(existingSubId, null),
                    existingInstance.Value.canonicalName);
            var existingMetadata = Path.Combine(existingPath, "metadata.json");

            if (File.Exists(existingMetadata))
            {
                _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(Create),
                    $"There's an existing instance of {TService.UniqueName} service existing with the name {instanceName}");
                return;
            }

            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(Create),
                $"Removing orphaned DNS entry for {TService.UniqueName}/{instanceName}: metadata file no longer exists at '{existingMetadata}'.");
            GlobalDnsEntries.DeleteEntry(TService.UniqueName, existingInstance.Value.subscription,
                existingInstance.Value.resourceGroup, instanceName);
        }

        var metadataFilePath = InitializeServiceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id);

        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(Create),
            $"Attempting to create {metadataFilePath} file.");

        if (File.Exists(metadataFilePath))
            throw new InvalidOperationException($"Metadata file for {typeof(TService)} with ID {id} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        if (!TService.IsGlobalService) return;

        GlobalDnsEntries.AddEntry(TService.UniqueName, subscriptionIdentifier.Value, resourceGroupIdentifier?.Value,
            instanceName);
    }

    private string InitializeServiceDirectories(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeServiceDirectories),
            $"Attempting to create {servicePath} directory...");

        if (!Directory.Exists(servicePath))
        {
            Directory.CreateDirectory(servicePath);
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeServiceDirectories),
                $"Directory {servicePath} created.");
        }
        else
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeServiceDirectories),
                $"Attempting to create {servicePath} directory - skipped.");
        }

        const string fileName = "metadata.json";
        var instancePath = string.IsNullOrWhiteSpace(id)
            ? Path.Combine(BaseEmulatorPath,
                GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier))
            : Path.Combine(BaseEmulatorPath,
                GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), id);
        var metadataFilePath = Path.Combine(instancePath, fileName);
        var dataPath = Path.Combine(instancePath, "data");

        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeServiceDirectories),
            $"Attempting to create {instancePath} directory.");
        if (Directory.Exists(instancePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeServiceDirectories),
                $"Attempting to create {instancePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(instancePath);
            Directory.CreateDirectory(dataPath);

            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeServiceDirectories),
                $"Attempting to create {instancePath} directory - created!");
        }

        return metadataFilePath;
    }

    public void CreateOrUpdate<TModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier, string? id, TModel model, bool createOperation = false,
        bool recoverInstance = false)
    {
        var instanceName = string.IsNullOrWhiteSpace(id)
            ? resourceGroupIdentifier == null
                ? subscriptionIdentifier.Value.ToString()
                : resourceGroupIdentifier.Value
            : id;

        var existingInstance = GlobalDnsEntries.GetEntry(TService.UniqueName, instanceName);
        if (existingInstance != null && TService.IsGlobalService && createOperation && !recoverInstance)
        {
            // Verify the resource the DNS entry points to still exists on disk.
            // If the parent resource (e.g. a resource group) was deleted, the directory is gone
            // but this service's DNS entry may remain as a stale orphan. Detect this and clean
            // up so that recreation can proceed normally.
            var existingSubId = SubscriptionIdentifier.From(existingInstance.Value.subscription);
            var existingRgId = existingInstance.Value.resourceGroup != null
                ? ResourceGroupIdentifier.From(existingInstance.Value.resourceGroup)
                : null;
            var existingPath = existingRgId != null
                ? GetServiceInstancePath(existingSubId, existingRgId, existingInstance.Value.canonicalName)
                : Path.Combine(BaseEmulatorPath,
                    GetLocalDirectoryPathWithReplacedValues(existingSubId, null),
                    existingInstance.Value.canonicalName);
            var existingMetadata = Path.Combine(existingPath, "metadata.json");

            if (File.Exists(existingMetadata))
            {
                _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(CreateOrUpdate),
                    $"There's an existing instance of {TService.UniqueName} service existing with the name {instanceName}");
                return;
            }

            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(CreateOrUpdate),
                $"Removing orphaned DNS entry for {TService.UniqueName}/{instanceName}: metadata file no longer exists at '{existingMetadata}'.");
            GlobalDnsEntries.DeleteEntry(TService.UniqueName, existingInstance.Value.subscription,
                existingInstance.Value.resourceGroup, instanceName);
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

    public string GetServiceInstancePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string? id)
    {
        var basePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        return string.IsNullOrWhiteSpace(id)
            ? basePath
            : Path.Combine(basePath, ResolveIdCasing(basePath, id));
    }

    public string GetServiceInstanceDataPath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id)
    {

        if (id.Contains("..") || id.Contains('/') || id.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");

        var basePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier));
        var resolvedId = ResolveIdCasing(basePath, id);

        return Path.Combine(basePath, resolvedId, "data");
    }

    private static void ThrowIfIdentifierContainsForbiddenExpressions(string identifier)
    {
        PathGuard.ValidateName(identifier);
    }

    public void CreateOrUpdateSubresource<TModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource, TModel model)
    {
        if (TService.Subresources == null)
        {
            throw new InvalidOperationException(
                "You can't create a subresource for a parent service which defines not subresources.");
        }

        if (!TService.Subresources.Contains(subresource))
        {
            throw new InvalidOperationException(
                $"You can't create a subresource '{subresource}' for a parent service which doesn't define that subresource.");
        }

        var metadataFilePath = InitializeSubresourceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id,
            parentId, subresource);

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);
    }

    private string InitializeSubresourceDirectories(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        const string metadataFile = "metadata.json";
        var subresourcePath =
            GetSubresourcePath(subscriptionIdentifier, resourceGroupIdentifier, parentId, id, subresource);
        var dataPath = Path.Combine(subresourcePath, "data");
        var metadataFilePath = Path.Combine(subresourcePath, metadataFile);

        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeSubresourceDirectories),
            $"Attempting to create {subresourcePath} directory.");
        if (!Directory.Exists(subresourcePath))
        {
            Directory.CreateDirectory(subresourcePath);
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeSubresourceDirectories),
                $"Attempting to create {subresourcePath} directory - created!");
        }
        else
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeSubresourceDirectories),
                $"Attempting to create {subresourcePath} directory - skipped.");
        }

        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeSubresourceDirectories),
            $"Attempting to create {metadataFilePath} file.");
        if (!Directory.Exists(metadataFilePath))
        {
            Directory.CreateDirectory(dataPath);
        }
        else
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(InitializeSubresourceDirectories),
                $"Attempting to create {metadataFilePath} directory - skipped.");
        }

        return metadataFilePath;
    }

    private string? GetSubresource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        if (TService.Subresources == null)
        {
            throw new InvalidOperationException(
                "You can't get a subresource for a parent service which defines not subresources.");
        }

        if (!TService.Subresources.Contains(subresource))
        {
            throw new InvalidOperationException(
                $"You can't get a subresource '{subresource}' for a parent service which doesn't define that subresource.");
        }

        var metadataFilePath = InitializeSubresourceDirectories(subscriptionIdentifier, resourceGroupIdentifier, id,
            parentId, subresource);

        if (!File.Exists(metadataFilePath)) return null;

        var content = File.ReadAllText(metadataFilePath);
        return string.IsNullOrEmpty(content)
            ? throw new InvalidOperationException("Metadata file is null or empty.")
            : content;
    }

    public T? GetSubresourceAs<T>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        var raw = GetSubresource(subscriptionIdentifier, resourceGroupIdentifier, id, parentId, subresource);
        if (string.IsNullOrEmpty(raw)) return default;
        var json = JsonSerializer.Deserialize<T>(raw, GlobalSettings.JsonOptions);

        return json;
    }

    private string GetSubresourcePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string parentId, string subresourceId, string subresource)
    {
        if (parentId.Contains("..") || parentId.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        if (subresourceId.Contains("..") || subresourceId.Contains('/') || subresourceId.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        if (subresource.Contains("..") || subresource.Contains('/') || subresource.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        return Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), parentId,
            subresource, subresourceId);
    }

    public void DeleteSubresource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string id, string parentId, string subresource)
    {
        var subresourcePath =
            GetSubresourcePath(subscriptionIdentifier, resourceGroupIdentifier, parentId, id, subresource);
        if (!Directory.Exists(subresourcePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(DeleteSubresource),
                $"The subresource '{subresourcePath}' does not exists, no changes applied.");
            return;
        }

        _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(DeleteSubresource),
            $"Deleting subresource '{subresourcePath}'.");
        Directory.Delete(subresourcePath, true);
    }

    public T[] ListSubresourcesAs<T>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string parentId, string subresource)
    {
        if (TService.Subresources == null)
        {
            throw new InvalidOperationException(
                "You can't get a subresource for a parent service which defines not subresources.");
        }

        if (!TService.Subresources.Contains(subresource))
        {
            throw new InvalidOperationException(
                $"You can't get a subresource '{subresource}' for a parent service which doesn't define that subresource.");
        }
        
        if (parentId.Contains("..") || parentId.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        if (subresource.Contains("..") || subresource.Contains('/') || subresource.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        var subresourcePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), parentId,
            subresource);

        if (!Directory.Exists(subresourcePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(ListSubresourcesAs),
                $"Subresource directory '{subresourcePath}' does not exist, returning empty list.");
            return [];
        }

        var metadataFiles = Directory.GetFiles(subresourcePath, "*.json", SearchOption.AllDirectories);

        return metadataFiles.Length == 0
            ? []
            : metadataFiles.Select(x => JsonSerializer.Deserialize<T>(File.ReadAllText(x), GlobalSettings.JsonOptions)!)
                .ToArray();
    }

    public T[] ListSubresourcesShallowAs<T>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string parentId, string subresource)
    {
        if (TService.Subresources == null)
        {
            throw new InvalidOperationException(
                "You can't get a subresource for a parent service which defines not subresources.");
        }

        if (!TService.Subresources.Contains(subresource))
        {
            throw new InvalidOperationException(
                $"You can't get a subresource '{subresource}' for a parent service which doesn't define that subresource.");
        }

        if (parentId.Contains("..") || parentId.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        if (subresource.Contains("..") || subresource.Contains('/') || subresource.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        var subresourcePath = Path.Combine(BaseEmulatorPath,
            GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), parentId,
            subresource);

        if (!Directory.Exists(subresourcePath))
        {
            _logger.LogDebug(nameof(ResourceProviderBase<>), nameof(ListSubresourcesShallowAs),
                $"Subresource directory '{subresourcePath}' does not exist, returning empty list.");
            return [];
        }

        // Read only the immediate subdirectory metadata files to avoid picking up
        // nested sub-resource files (e.g. rules stored under each subscription folder).
        var metadataFiles = Directory.GetDirectories(subresourcePath)
            .Select(d => Path.Combine(d, "metadata.json"))
            .Where(File.Exists)
            .ToArray();

        return metadataFiles.Length == 0
            ? []
            : metadataFiles.Select(x => JsonSerializer.Deserialize<T>(File.ReadAllText(x), GlobalSettings.JsonOptions)!)
                .ToArray();
    }
}