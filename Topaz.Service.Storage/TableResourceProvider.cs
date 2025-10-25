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
        _logger.LogDebug($"[{nameof(TableResourceProvider)}.{nameof(CheckIfTableExists)}]: Executing for {subscriptionIdentifier}, {resourceGroupIdentifier}, {tableName}, {storageAccountName}");
        
        var tablePath = GetTablePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);
        return Directory.Exists(tablePath);
    }

    private string GetTablePathWithReplacedValues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName)
    {
        var storageAccountPath =
            GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return Path.Combine(storageAccountPath, ".table", tableName);
    }

    public void Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName, TableItem model)
    {
        base.Create(subscriptionIdentifier, resourceGroupIdentifier, GetTableId(storageAccountName, tableName), model);

        var aclPath = Path.Combine(GetTablePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName), "acl");
        Directory.CreateDirectory(aclPath);
    }

    private static string GetTableId(string storageAccountName, string tableName)
    {
        return Path.Combine(storageAccountName, ".table", tableName);
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        base.Delete(subscriptionIdentifier, resourceGroupIdentifier, GetTableId(storageAccountName, tableName));
    }

    public string GetTableDataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        return Path.Combine(GetTablePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName), "data");
    }
}
