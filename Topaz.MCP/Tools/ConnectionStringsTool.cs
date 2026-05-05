using System.ComponentModel;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Returns ready-to-use connection strings and URIs for all provisioned resources.")]
[UsedImplicitly]
public sealed class ConnectionStringsTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Queries the running Topaz instance and returns connection strings and URIs for all provisioned resources in the given subscription (storage accounts, Service Bus namespaces, Key Vaults, Event Hub namespaces, Container Registries).")]
    [UsedImplicitly]
    public static async Task<ConnectionStringsResult> GetConnectionStrings(
        [Description("ID of the subscription to query.")]
        Guid subscriptionId,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);

        var storageEntries = new List<StorageConnectionStringEntry>();
        var serviceBusEntries = new List<ServiceBusConnectionStringEntry>();
        var keyVaultEntries = new List<KeyVaultUriEntry>();
        var eventHubEntries = new List<EventHubConnectionStringEntry>();
        var registryEntries = new List<ContainerRegistryEntry>();

        await foreach (var resourceGroup in subscription.GetResourceGroups().GetAllAsync().ConfigureAwait(false))
        {
            // Storage accounts
            await foreach (var sa in resourceGroup.GetStorageAccounts().GetAllAsync().ConfigureAwait(false))
            {
                var keys = new List<string>();
                await foreach (var key in sa.GetKeysAsync().ConfigureAwait(false))
                {
                    keys.Add(key.Value!);
                }

                var primaryKey = keys.Count > 0 ? keys[0] : string.Empty;
                storageEntries.Add(new StorageConnectionStringEntry
                {
                    ResourceGroup = resourceGroup.Data.Name,
                    AccountName = sa.Data.Name,
                    ConnectionString = TopazResourceHelpers.GetAzureStorageConnectionString(sa.Data.Name, primaryKey),
                    BlobServiceUri = TopazResourceHelpers.GetBlobServiceUri(sa.Data.Name),
                    QueueServiceUri = TopazResourceHelpers.GetQueueServiceUri(sa.Data.Name),
                    TableServiceUri = TopazResourceHelpers.GetTableServiceUri(sa.Data.Name),
                });
            }

            // Service Bus namespaces
            await foreach (var ns in resourceGroup.GetServiceBusNamespaces().GetAllAsync().ConfigureAwait(false))
            {
                serviceBusEntries.Add(new ServiceBusConnectionStringEntry
                {
                    ResourceGroup = resourceGroup.Data.Name,
                    NamespaceName = ns.Data.Name,
                    ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(ns.Data.Name),
                    ConnectionStringWithTls = TopazResourceHelpers.GetServiceBusConnectionStringWithTls(ns.Data.Name),
                });
            }

            // Key Vaults
            await foreach (var kv in resourceGroup.GetKeyVaults().GetAllAsync().ConfigureAwait(false))
            {
                keyVaultEntries.Add(new KeyVaultUriEntry
                {
                    ResourceGroup = resourceGroup.Data.Name,
                    VaultName = kv.Data.Name,
                    VaultUri = TopazResourceHelpers.GetKeyVaultEndpoint(kv.Data.Name).ToString(),
                });
            }

            // Event Hub namespaces
            await foreach (var ehns in resourceGroup.GetEventHubsNamespaces().GetAllAsync().ConfigureAwait(false))
            {
                eventHubEntries.Add(new EventHubConnectionStringEntry
                {
                    ResourceGroup = resourceGroup.Data.Name,
                    NamespaceName = ehns.Data.Name,
                    ConnectionString = TopazResourceHelpers.GetEventHubConnectionString(ehns.Data.Name),
                });
            }

            // Container Registries
            await foreach (var acr in resourceGroup.GetContainerRegistries().GetAllAsync().ConfigureAwait(false))
            {
                registryEntries.Add(new ContainerRegistryEntry
                {
                    ResourceGroup = resourceGroup.Data.Name,
                    RegistryName = acr.Data.Name,
                    LoginServer = TopazResourceHelpers.GetContainerRegistryLoginServer(acr.Data.Name),
                });
            }
        }

        return new ConnectionStringsResult
        {
            StorageAccounts = storageEntries,
            ServiceBusNamespaces = serviceBusEntries,
            KeyVaults = keyVaultEntries,
            EventHubNamespaces = eventHubEntries,
            ContainerRegistries = registryEntries,
        };
    }

    public sealed record ConnectionStringsResult
    {
        public required List<StorageConnectionStringEntry> StorageAccounts { [UsedImplicitly] get; init; }
        public required List<ServiceBusConnectionStringEntry> ServiceBusNamespaces { [UsedImplicitly] get; init; }
        public required List<KeyVaultUriEntry> KeyVaults { [UsedImplicitly] get; init; }
        public required List<EventHubConnectionStringEntry> EventHubNamespaces { [UsedImplicitly] get; init; }
        public required List<ContainerRegistryEntry> ContainerRegistries { [UsedImplicitly] get; init; }
    }

    public sealed record StorageConnectionStringEntry
    {
        public required string ResourceGroup { [UsedImplicitly] get; init; }
        public required string AccountName { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
        public required string BlobServiceUri { [UsedImplicitly] get; init; }
        public required string QueueServiceUri { [UsedImplicitly] get; init; }
        public required string TableServiceUri { [UsedImplicitly] get; init; }
    }

    public sealed record ServiceBusConnectionStringEntry
    {
        public required string ResourceGroup { [UsedImplicitly] get; init; }
        public required string NamespaceName { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
        public required string ConnectionStringWithTls { [UsedImplicitly] get; init; }
    }

    public sealed record KeyVaultUriEntry
    {
        public required string ResourceGroup { [UsedImplicitly] get; init; }
        public required string VaultName { [UsedImplicitly] get; init; }
        public required string VaultUri { [UsedImplicitly] get; init; }
    }

    public sealed record EventHubConnectionStringEntry
    {
        public required string ResourceGroup { [UsedImplicitly] get; init; }
        public required string NamespaceName { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
    }

    public sealed record ContainerRegistryEntry
    {
        public required string ResourceGroup { [UsedImplicitly] get; init; }
        public required string RegistryName { [UsedImplicitly] get; init; }
        public required string LoginServer { [UsedImplicitly] get; init; }
    }
}
