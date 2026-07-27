using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using NUnit.Framework;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateStorageToolTests
{
    private const string AccountName = "mcpcreatestorage";
    private const string ContainerName = "mcp-test-container";
    private const string QueueName = "mcp-test-queue";
    private const string TableName = "mcptesttable";

    [OneTimeSetUp]
    public async Task CreateStorageAccount()
    {
        // Delete any leftover state from previous runs so creation is always clean.
        await Program.RunAsync([
            "storage", "account", "delete",
            "--name", AccountName,
            "-g", McpTestFixture.ResourceGroupName,
            "--subscription-id", McpTestFixture.SubscriptionId.ToString()
        ]);

        await CreateStorageTool.CreateStorageAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "westeurope",
            McpTestFixture.ObjectId);
    }

    [Test]
    public async Task CreateStorageAccount_ReturnsAccountName()
    {
        var result = await CreateStorageTool.CreateStorageAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.AccountName, Is.EqualTo(AccountName));
    }

    [Test]
    public async Task CreateStorageAccount_ReturnsConnectionStringWithAccountName()
    {
        var result = await CreateStorageTool.CreateStorageAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.ConnectionString, Does.Contain($"AccountName={AccountName}"));
    }

    [Test]
    public async Task CreateStorageAccount_ReturnsBlobServiceUri()
    {
        var result = await CreateStorageTool.CreateStorageAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.BlobServiceUri,
            Does.Contain($"{AccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}"));
    }

    [Test]
    public async Task CreateStorageAccount_ReturnsQueueServiceUri()
    {
        var result = await CreateStorageTool.CreateStorageAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.QueueServiceUri,
            Does.Contain($"{AccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}"));
    }

    [Test]
    public async Task CreateStorageAccount_ReturnsTableServiceUri()
    {
        var result = await CreateStorageTool.CreateStorageAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.TableServiceUri,
            Does.Contain($"{AccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}"));
    }

    [Test]
    public async Task CreateBlobContainer_ReturnsContainerName()
    {
        var result = await CreateStorageTool.CreateBlobContainer(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            ContainerName,
            McpTestFixture.ObjectId);

        Assert.That(result.ContainerName, Is.EqualTo(ContainerName));
    }

    [Test]
    public async Task CreateStorageQueue_ReturnsQueueName()
    {
        var result = await CreateStorageTool.CreateStorageQueue(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            QueueName,
            McpTestFixture.ObjectId);

        Assert.That(result.QueueName, Is.EqualTo(QueueName));
    }

    [Test]
    public async Task CreateStorageQueue_ReturnsQueueServiceUri()
    {
        var result = await CreateStorageTool.CreateStorageQueue(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            QueueName,
            McpTestFixture.ObjectId);

        Assert.That(result.QueueServiceUri,
            Does.Contain($"{AccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}"));
    }

    [Test]
    public async Task CreateStorageQueue_QueueExistsViaArmSdk()
    {
        await CreateStorageTool.CreateStorageQueue(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            QueueName,
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var storageAccount = await rg.Value.GetStorageAccountAsync(AccountName);
        var queue = await storageAccount.Value.GetQueueService().GetStorageQueueAsync(QueueName);

        Assert.That(queue.Value.Data.Name, Is.EqualTo(QueueName));
    }

    [Test]
    public async Task CreateStorageTable_ReturnsTableName()
    {
        var result = await CreateStorageTool.CreateStorageTable(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            TableName,
            McpTestFixture.ObjectId);

        Assert.That(result.TableName, Is.EqualTo(TableName));
    }

    [Test]
    public async Task CreateStorageTable_ReturnsTableServiceUri()
    {
        var result = await CreateStorageTool.CreateStorageTable(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            TableName,
            McpTestFixture.ObjectId);

        Assert.That(result.TableServiceUri,
            Does.Contain($"{AccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}"));
    }

    [Test]
    public async Task CreateStorageTable_TableExistsViaArmSdk()
    {
        await CreateStorageTool.CreateStorageTable(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            TableName,
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var storageAccount = await rg.Value.GetStorageAccountAsync(AccountName);
        var table = await storageAccount.Value.GetTableService().GetTableAsync(TableName);

        Assert.That(table.Value.Data.Name, Is.EqualTo(TableName));
    }

    [Test]
    public async Task CreateBlobContainer_ContainerExistsInStorageAccount()
    {
        await CreateStorageTool.CreateBlobContainer(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            ContainerName,
            McpTestFixture.ObjectId);

        // Verify via ARM SDK.
        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var storageAccount = await rg.Value.GetStorageAccountAsync(AccountName);
        var container = await storageAccount.Value.GetBlobService().GetBlobContainerAsync(ContainerName);

        Assert.That(container.Value.Data.Name, Is.EqualTo(ContainerName));
    }
}
