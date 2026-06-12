using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure Cosmos DB resources (accounts, databases, containers) in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateCosmosDbTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Cosmos DB account (DatabaseAccount) with SQL API in the given resource group.")]
    [UsedImplicitly]
    public static async Task<CosmosDbAccountResult> CreateCosmosDbAccount(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the Cosmos DB account will be created.")]
        string resourceGroupName,
        [Description("Name of the Cosmos DB account to create.")]
        string accountName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var locations = new[] { new CosmosDBAccountLocation { LocationName = location, FailoverPriority = 0 } };
        var content = new CosmosDBAccountCreateOrUpdateContent(new AzureLocation(location), locations)
        {
            Kind = CosmosDBAccountKind.GlobalDocumentDB,
        };

        var operation = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, content)
            .ConfigureAwait(false);

        var account = operation.Value;
        var keys = await account.GetKeysAsync().ConfigureAwait(false);
        var primaryKey = keys.Value.PrimaryMasterKey ?? string.Empty;
        var accountEndpoint = TopazResourceHelpers.GetCosmosDbAccountEndpoint(accountName);

        return new CosmosDbAccountResult
        {
            Name = account.Data.Name,
            ResourceId = account.Data.Id?.ToString(),
            AccountEndpoint = accountEndpoint,
            PrimaryConnectionString = TopazResourceHelpers.GetCosmosDbConnectionString(accountName, primaryKey),
            ProvisioningState = account.Data.ProvisioningState?.ToString(),
        };
    }

    [McpServerTool]
    [Description("Creates a SQL database inside an existing Cosmos DB account.")]
    [UsedImplicitly]
    public static async Task<CosmosDbDatabaseResult> CreateCosmosDbDatabase(
        [Description("ID of the subscription containing the Cosmos DB account.")]
        Guid subscriptionId,
        [Description("Name of the resource group containing the Cosmos DB account.")]
        string resourceGroupName,
        [Description("Name of the Cosmos DB account.")]
        string accountName,
        [Description("Name of the SQL database to create.")]
        string databaseName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Optional throughput (RU/s) for the database. If not specified, uses default serverless.")]
        int? throughput = null)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);
        var account = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(accountName).ConfigureAwait(false);

        var resource = new CosmosDBSqlDatabaseResourceInfo(databaseName);
        var content = new CosmosDBSqlDatabaseCreateOrUpdateContent(new AzureLocation(account.Value.Data.Location), resource);

        var operation = await account.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, content)
            .ConfigureAwait(false);

        var database = operation.Value;

        return new CosmosDbDatabaseResult
        {
            Name = database.Data.Name,
            ResourceId = database.Data.Id?.ToString(),
        };
    }

    [McpServerTool]
    [Description("Creates a SQL container inside an existing Cosmos DB database.")]
    [UsedImplicitly]
    public static async Task<CosmosDbContainerResult> CreateCosmosDbContainer(
        [Description("ID of the subscription containing the Cosmos DB account.")]
        Guid subscriptionId,
        [Description("Name of the resource group containing the Cosmos DB account.")]
        string resourceGroupName,
        [Description("Name of the Cosmos DB account.")]
        string accountName,
        [Description("Name of the SQL database.")]
        string databaseName,
        [Description("Name of the SQL container to create.")]
        string containerName,
        [Description("The partition key path (e.g. '/id' or '/tenantId').")]
        string partitionKeyPath,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Optional throughput (RU/s) for the container. If not specified, uses default serverless.")]
        int? throughput = null)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);
        var account = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(accountName).ConfigureAwait(false);
        var database = await account.Value.GetCosmosDBSqlDatabases().GetAsync(databaseName).ConfigureAwait(false);

        var partitionKey = new CosmosDBContainerPartitionKey
        {
            Paths = { partitionKeyPath },
            Kind = "Hash",
        };

        var resource = new CosmosDBSqlContainerResourceInfo(containerName)
        {
            PartitionKey = partitionKey,
        };
        var content = new CosmosDBSqlContainerCreateOrUpdateContent(
            new AzureLocation(account.Value.Data.Location), resource);

        var operation = await database.Value.GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName, content)
            .ConfigureAwait(false);

        var container = operation.Value;

        return new CosmosDbContainerResult
        {
            Name = container.Data.Name,
            ResourceId = container.Data.Id?.ToString(),
            PartitionKeyPath = partitionKeyPath,
        };
    }

    public sealed record CosmosDbAccountResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string AccountEndpoint { [UsedImplicitly] get; init; }
        public required string PrimaryConnectionString { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }

    public sealed record CosmosDbDatabaseResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
    }

    public sealed record CosmosDbContainerResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string PartitionKeyPath { [UsedImplicitly] get; init; }
    }
}
