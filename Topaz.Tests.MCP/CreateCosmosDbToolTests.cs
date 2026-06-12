using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using NUnit.Framework;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateCosmosDbToolTests
{
    private const string AccountName = "mcp-cosmos-account";
    private const string DatabaseName = "mcp-testdb";
    private const string ContainerName = "mcp-testcontainer";
    private const string PartitionKeyPath = "/tenantId";

    [OneTimeSetUp]
    public async Task DeleteLeftoverAccount()
    {
        // Delete any leftover state from previous runs so creation is always clean.
        await Program.RunAsync([
            "cosmos", "account", "delete",
            "--name", AccountName,
            "-g", McpTestFixture.ResourceGroupName,
            "--subscription-id", McpTestFixture.SubscriptionId.ToString()
        ]);
    }

    [Test, Order(1)]
    public async Task CreateCosmosDbAccount_ReturnsAccountName()
    {
        var result = await CreateCosmosDbTool.CreateCosmosDbAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(AccountName));
    }

    [Test, Order(1)]
    public async Task CreateCosmosDbAccount_ReturnsAccountEndpoint()
    {
        var result = await CreateCosmosDbTool.CreateCosmosDbAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.AccountEndpoint,
            Does.StartWith($"https://{AccountName}.{GlobalSettings.DocumentsDnsSuffix}:{GlobalSettings.DefaultCosmosDbPort}/"));
    }

    [Test, Order(1)]
    public async Task CreateCosmosDbAccount_ReturnsConnectionString()
    {
        var result = await CreateCosmosDbTool.CreateCosmosDbAccount(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.Multiple(() =>
        {
            Assert.That(result.PrimaryConnectionString, Does.Contain("AccountEndpoint="));
            Assert.That(result.PrimaryConnectionString, Does.Contain("AccountKey="));
            Assert.That(result.PrimaryConnectionString, Does.Contain(AccountName));
        });
    }

    [Test, Order(2)]
    public async Task CreateCosmosDbDatabase_ReturnsDatabase()
    {
        var result = await CreateCosmosDbTool.CreateCosmosDbDatabase(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            DatabaseName,
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(DatabaseName));
    }

    [Test, Order(2)]
    public async Task CreateCosmosDbDatabase_DatabaseExistsViaArmSdk()
    {
        await CreateCosmosDbTool.CreateCosmosDbDatabase(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            DatabaseName,
            McpTestFixture.ObjectId);

        var credential = new AzureLocalCredential(McpTestFixture.ObjectId);
        var armClient = new ArmClient(credential, McpTestFixture.SubscriptionId.ToString(), McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var account = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(AccountName);
        var database = await account.Value.GetCosmosDBSqlDatabases().GetAsync(DatabaseName);

        Assert.That(database.Value.Data.Name, Is.EqualTo(DatabaseName));
    }

    [Test, Order(3)]
    public async Task CreateCosmosDbContainer_ReturnsContainer()
    {
        var result = await CreateCosmosDbTool.CreateCosmosDbContainer(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            DatabaseName,
            ContainerName,
            PartitionKeyPath,
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(ContainerName));
    }

    [Test, Order(3)]
    public async Task CreateCosmosDbContainer_ReturnsPartitionKeyPath()
    {
        var result = await CreateCosmosDbTool.CreateCosmosDbContainer(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            DatabaseName,
            ContainerName,
            PartitionKeyPath,
            McpTestFixture.ObjectId);

        Assert.That(result.PartitionKeyPath, Is.EqualTo(PartitionKeyPath));
    }

    [Test, Order(3)]
    public async Task CreateCosmosDbContainer_ContainerExistsViaArmSdk()
    {
        await CreateCosmosDbTool.CreateCosmosDbContainer(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            AccountName,
            DatabaseName,
            ContainerName,
            PartitionKeyPath,
            McpTestFixture.ObjectId);

        var credential = new AzureLocalCredential(McpTestFixture.ObjectId);
        var armClient = new ArmClient(credential, McpTestFixture.SubscriptionId.ToString(), McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var account = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(AccountName);
        var database = await account.Value.GetCosmosDBSqlDatabases().GetAsync(DatabaseName);
        var container = await database.Value.GetCosmosDBSqlContainers().GetAsync(ContainerName);

        Assert.That(container.Value.Data.Name, Is.EqualTo(ContainerName));
    }
}
