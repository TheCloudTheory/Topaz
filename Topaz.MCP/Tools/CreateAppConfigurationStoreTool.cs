using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure App Configuration resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateAppConfigurationStoreTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates an App Configuration store in the given resource group and returns the endpoint URL and primary read-write connection string.")]
    [UsedImplicitly]
    public static async Task<AppConfigurationStoreResult> CreateAppConfigurationStore(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the store will be created.")]
        string resourceGroupName,
        [Description("Name of the App Configuration store to create.")]
        string storeName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var content = new AppConfigurationStoreData(new AzureLocation(location), new AppConfigurationSku("free"));
        var operation = await resourceGroup.Value.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, content)
            .ConfigureAwait(false);

        var store = operation.Value;

        string connectionString = string.Empty;
        await foreach (var key in store.GetKeysAsync().ConfigureAwait(false))
        {
            if (key.IsReadOnly == false)
            {
                connectionString = key.ConnectionString ?? string.Empty;
                break;
            }
        }

        return new AppConfigurationStoreResult
        {
            Name = store.Data.Name,
            ResourceId = store.Data.Id?.ToString(),
            Endpoint = GlobalSettings.GetAppConfigurationEndpoint(storeName),
            PrimaryReadWriteConnectionString = connectionString,
            ProvisioningState = store.Data.ProvisioningState?.ToString(),
        };
    }

    public sealed record AppConfigurationStoreResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string Endpoint { [UsedImplicitly] get; init; }
        public required string PrimaryReadWriteConnectionString { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }
}
