using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class ConnectionStringsToolTests
{
    private const string StorageAccountName = "mcpteststorage";
    private const string ServiceBusNamespaceName = "mcp-test-sb";
    private const string KeyVaultName = "mcp-test-kv";
    private const string EventHubNamespaceName = "mcp-test-eh";
    private const string RegistryName = "mcptestreg";
    private const string CosmosDbAccountName = "mcp-test-cosmos";
    private const string AppConfigStoreName = "mcp-test-appconfig";

    [OneTimeSetUp]
    public async Task ProvisionResources()
    {
        var credential = new AzureLocalCredential(McpTestFixture.ObjectId);
        var armClient = new ArmClient(credential, McpTestFixture.SubscriptionId.ToString(), McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var rg = resourceGroup.Value;

        // Storage account
        await rg.GetStorageAccounts().CreateOrUpdateAsync(
            WaitUntil.Completed,
            StorageAccountName,
            new StorageAccountCreateOrUpdateContent(
                new StorageSku(StorageSkuName.StandardLrs),
                StorageKind.StorageV2,
                McpTestFixture.Location));

        // Service Bus namespace
        await rg.GetServiceBusNamespaces().CreateOrUpdateAsync(
            WaitUntil.Completed,
            ServiceBusNamespaceName,
            new ServiceBusNamespaceData(McpTestFixture.Location));

        // Key Vault
        await rg.GetKeyVaults().CreateOrUpdateAsync(
            WaitUntil.Completed,
            KeyVaultName,
            new KeyVaultCreateOrUpdateContent(
                McpTestFixture.Location,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

        // Event Hub namespace
        await rg.GetEventHubsNamespaces().CreateOrUpdateAsync(
            WaitUntil.Completed,
            EventHubNamespaceName,
            new EventHubsNamespaceData(McpTestFixture.Location));

        // Container Registry
        await rg.GetContainerRegistries().CreateOrUpdateAsync(
            WaitUntil.Completed,
            RegistryName,
            new ContainerRegistryData(
                McpTestFixture.Location,
                new ContainerRegistrySku(ContainerRegistrySkuName.Basic)));

        // Cosmos DB account
        await CreateCosmosDbTool.CreateCosmosDbAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            CosmosDbAccountName,
            "eastus",
            McpTestFixture.ObjectId);

        // App Configuration store
        await rg.GetAppConfigurationStores().CreateOrUpdateAsync(
            WaitUntil.Completed,
            AppConfigStoreName,
            new AppConfigurationStoreData(McpTestFixture.Location, new AppConfigurationSku("free")));
    }

    [Test]
    public async Task GetConnectionStrings_StorageAccount_ReturnsCorrectConnectionString()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.StorageAccounts.Single(s => s.AccountName == StorageAccountName);

        Assert.Multiple(() =>
        {
            Assert.That(entry.ConnectionString, Does.Contain($"AccountName={StorageAccountName}"));
            Assert.That(entry.ConnectionString, Does.Contain("DefaultEndpointsProtocol=http"));
            Assert.That(entry.BlobServiceUri, Does.Contain($"{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}"));
            Assert.That(entry.QueueServiceUri, Does.Contain($"{StorageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}"));
            Assert.That(entry.TableServiceUri, Does.Contain($"{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}"));
        });
    }

    [Test]
    public async Task GetConnectionStrings_ServiceBusNamespace_ReturnsCorrectConnectionString()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.ServiceBusNamespaces.Single(s => s.NamespaceName == ServiceBusNamespaceName);

        Assert.Multiple(() =>
        {
            Assert.That(entry.ConnectionString, Does.Contain($"{ServiceBusNamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.DefaultServiceBusAmqpPort}"));
            Assert.That(entry.ConnectionStringWithTls, Does.Contain($"{ServiceBusNamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort}"));
            Assert.That(entry.ConnectionString, Does.Contain("SharedAccessKeyName=RootManageSharedAccessKey"));
        });
    }

    [Test]
    public async Task GetConnectionStrings_KeyVault_ReturnsCorrectUri()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.KeyVaults.Single(k => k.VaultName == KeyVaultName);

        Assert.That(entry.VaultUri,
            Does.StartWith($"https://{KeyVaultName}.{GlobalSettings.KeyVaultDnsSuffix}:{GlobalSettings.DefaultKeyVaultPort}"));
    }

    [Test]
    public async Task GetConnectionStrings_EventHubNamespace_ReturnsCorrectConnectionString()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.EventHubNamespaces.Single(e => e.NamespaceName == EventHubNamespaceName);

        Assert.Multiple(() =>
        {
            Assert.That(entry.ConnectionString, Does.Contain($"{EventHubNamespaceName}.eventhub.topaz.local.dev:{GlobalSettings.DefaultEventHubAmqpPort}"));
            Assert.That(entry.ConnectionString, Does.Contain("SharedAccessKeyName=RootManageSharedAccessKey"));
        });
    }

    [Test]
    public async Task GetConnectionStrings_ContainerRegistry_ReturnsCorrectLoginServer()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.ContainerRegistries.Single(r => r.RegistryName == RegistryName);

        Assert.That(entry.LoginServer, Is.EqualTo($"{RegistryName}.cr.topaz.local.dev:8892"));
    }

    [Test]
    public async Task GetConnectionStrings_CosmosDbAccount_ReturnsCorrectAccountEndpoint()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.CosmosDbAccounts?.Single(c => c.AccountName == CosmosDbAccountName);

        Assert.That(entry?.AccountEndpoint,
            Does.StartWith($"https://{CosmosDbAccountName}.{GlobalSettings.DocumentsDnsSuffix}:{GlobalSettings.DefaultCosmosDbPort}/"));
    }

    [Test]
    public async Task GetConnectionStrings_CosmosDbAccount_ReturnsCorrectConnectionString()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.CosmosDbAccounts?.Single(c => c.AccountName == CosmosDbAccountName);

        Assert.Multiple(() =>
        {
            Assert.That(entry?.PrimaryConnectionString, Does.Contain("AccountEndpoint="));
            Assert.That(entry?.PrimaryConnectionString, Does.Contain("AccountKey="));
            Assert.That(entry?.PrimaryConnectionString, Does.Contain(CosmosDbAccountName));
        });
    }

    [Test]
    public async Task GetConnectionStrings_AppConfigurationStore_ReturnsCorrectEndpointAndConnectionString()
    {
        var result = await ConnectionStringsTool.GetConnectionStrings(McpTestFixture.SubscriptionId, McpTestFixture.ObjectId);

        var entry = result.AppConfigurationStores.Single(s => s.StoreName == AppConfigStoreName);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Endpoint,
                Does.StartWith($"https://{AppConfigStoreName}.{GlobalSettings.AppConfigurationDnsSuffix}:{GlobalSettings.DefaultAppConfigurationPort}/"));
            Assert.That(entry.PrimaryReadWriteConnectionString, Does.Contain("Endpoint="));
            Assert.That(entry.PrimaryReadWriteConnectionString, Does.Contain("Id="));
            Assert.That(entry.PrimaryReadWriteConnectionString, Does.Contain("Secret="));
            Assert.That(entry.PrimaryReadWriteConnectionString, Does.Contain(AppConfigStoreName));
        });
    }
}
