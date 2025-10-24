using Azure.Data.Tables.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class TableResourceProvider(ITopazLogger logger) : ResourceProviderBase<TableStorageService>(logger)
{
    private readonly ITopazLogger _logger = logger;

    public string GetTableAclPath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        var tablePath =
            GetTablePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);
        
        return Path.Combine(tablePath, "acl");
    }

    public bool CheckIfTableExists(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        var tablePath = GetTablePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);
        return Directory.Exists(tablePath);
    }

    private string GetTablePathWithReplacedValues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName)
    {
        var storageAccountPath =
            GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return Path.Combine(storageAccountPath, tableName);
    }

    public void Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName, TableItem model)
    {
        base.Create(subscriptionIdentifier, resourceGroupIdentifier, GetTableId(storageAccountName, tableName), model);
    }

    private static string GetTableId(string storageAccountName, string tableName)
    {
        return Path.Combine(storageAccountName, ".table", tableName);
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        base.Delete(subscriptionIdentifier, resourceGroupIdentifier, GetTableId(storageAccountName, tableName));
    }

    public IEnumerable<string> List(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), storageAccountName);
        if (!Directory.Exists(servicePath))
        {
            _logger.LogWarning("Trying to list resources for a non-existing storage account. If you see this warning, make sure you created a storage account before accessing its data.");
            return [];
        }
        
        var metadataFiles = Directory.EnumerateFiles(servicePath, "metadata.json", SearchOption.AllDirectories);
        return metadataFiles.Select(File.ReadAllText);
    }

    public string GetTableDataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        return GetTablePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);
    }
}
