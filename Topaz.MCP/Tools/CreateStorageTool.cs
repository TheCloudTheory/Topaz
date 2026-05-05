using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates and manages Azure Storage resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateStorageTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Storage Account in the given resource group and returns its connection strings.")]
    [UsedImplicitly]
    public static async Task<StorageAccountResult> CreateStorageAccount(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the storage account to create (lowercase, 3–24 characters).")]
        string storageAccountName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var sku = new StorageSku(StorageSkuName.StandardLrs);
        var content = new StorageAccountCreateOrUpdateContent(sku, StorageKind.StorageV2, new AzureLocation(location));
        var storageAccount = await resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, storageAccountName, content)
            .ConfigureAwait(false);

        var keys = new List<string>();
        await foreach (var key in storageAccount.Value.GetKeysAsync().ConfigureAwait(false))
        {
            keys.Add(key.Value!);
        }

        var primaryKey = keys.Count > 0 ? keys[0] : string.Empty;
        return new StorageAccountResult
        {
            AccountName = storageAccountName,
            ConnectionString = TopazResourceHelpers.GetAzureStorageConnectionString(storageAccountName, primaryKey),
            BlobServiceUri = TopazResourceHelpers.GetBlobServiceUri(storageAccountName),
            QueueServiceUri = TopazResourceHelpers.GetQueueServiceUri(storageAccountName),
            TableServiceUri = TopazResourceHelpers.GetTableServiceUri(storageAccountName),
        };
    }

    [McpServerTool]
    [Description("Creates a Blob container inside an existing Storage Account.")]
    [UsedImplicitly]
    public static async Task<BlobContainerResult> CreateBlobContainer(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the storage account.")]
        string storageAccountName,
        [Description("Name of the blob container to create.")]
        string containerName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(storageAccountName).ConfigureAwait(false);

        await storageAccount.Value.GetBlobService().GetBlobContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName, new BlobContainerData())
            .ConfigureAwait(false);

        return new BlobContainerResult
        {
            AccountName = storageAccountName,
            ContainerName = containerName,
            BlobServiceUri = TopazResourceHelpers.GetBlobServiceUri(storageAccountName),
        };
    }

    public sealed record StorageAccountResult
    {
        public required string AccountName { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
        public required string BlobServiceUri { [UsedImplicitly] get; init; }
        public required string QueueServiceUri { [UsedImplicitly] get; init; }
        public required string TableServiceUri { [UsedImplicitly] get; init; }
    }

    public sealed record BlobContainerResult
    {
        public required string AccountName { [UsedImplicitly] get; init; }
        public required string ContainerName { [UsedImplicitly] get; init; }
        public required string BlobServiceUri { [UsedImplicitly] get; init; }
    }
}
