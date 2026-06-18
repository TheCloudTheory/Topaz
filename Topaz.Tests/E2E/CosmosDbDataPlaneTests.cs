using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class CosmosDbDataPlaneTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003300");

    private const string SubscriptionName = "sub-test-cosmosdb-dp";
    private const string ResourceGroupName = "rg-test-cosmosdb-dp";
    private const string AccountName = "test-cosmos-dp";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group", "delete",
            "--name", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static CosmosDBAccountCreateOrUpdateContent MinimalAccountContent() =>
        new(AzureLocation.WestEurope, [new CosmosDBAccountLocation { LocationName = "westeurope" }]);

    private static CosmosClient CreateCosmosClient(string endpoint, string primaryKey) =>
        new(endpoint, primaryKey, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }),
            LimitToEndpoint = true
        });

    private async Task<(string Endpoint, string PrimaryKey)> CreateAccountAndGetCredentials()
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, AccountName, MinimalAccountContent());

        var account = accountResult.Value;
        var endpoint = account.Data.DocumentEndpoint;
        var keys = await account.GetKeysAsync();
        var primaryKey = keys.Value.PrimaryMasterKey;

        return (endpoint, primaryKey);
    }

    [Test]
    public async Task Database_WhenCreated_ReturnsCorrectId()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        const string databaseName = "dp-create-test";

        // Act
        var result = await client.CreateDatabaseAsync(databaseName);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
        Assert.That(result.Resource.Id, Is.EqualTo(databaseName));
    }

    [Test]
    public async Task Database_WhenRead_ReturnsCorrectId()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        const string databaseName = "dp-get-test";

        await client.CreateDatabaseAsync(databaseName);

        // Act
        var result = await client.GetDatabase(databaseName).ReadAsync();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(result.Resource.Id, Is.EqualTo(databaseName));
    }

    [Test]
    public async Task Database_WhenDeleted_CannotBeRead()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        const string databaseName = "dp-delete-test";

        await client.CreateDatabaseAsync(databaseName);

        // Act
        await client.GetDatabase(databaseName).DeleteAsync();

        // Assert
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.GetDatabase(databaseName).ReadAsync());
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Databases_WhenListed_ContainsCreatedDatabases()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);

        await client.CreateDatabaseAsync("dp-list-a");
        await client.CreateDatabaseAsync("dp-list-b");

        // Act
        var iterator = client.GetDatabaseQueryIterator<DatabaseProperties>();
        var names = new List<string>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            foreach (var db in page)
                names.Add(db.Id);
        }

        // Assert
        Assert.That(names, Does.Contain("dp-list-a"));
        Assert.That(names, Does.Contain("dp-list-b"));
    }

    [Test]
    public async Task Container_WhenCreated_ReturnsCorrectId()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync("dp-coll-create-db");

        // Act
        var result = await db.Database.CreateContainerAsync(new ContainerProperties("dp-coll-create", "/pk"));

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
        Assert.That(result.Resource.Id, Is.EqualTo("dp-coll-create"));
    }

    [Test]
    public async Task Container_WhenRead_ReturnsCorrectId()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync("dp-coll-read-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("dp-coll-read", "/pk"));

        // Act
        var result = await db.Database.GetContainer("dp-coll-read").ReadContainerAsync();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(result.Resource.Id, Is.EqualTo("dp-coll-read"));
    }

    [Test]
    public async Task Container_WhenDeleted_CannotBeRead()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync("dp-coll-delete-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("dp-coll-delete", "/pk"));

        // Act
        await db.Database.GetContainer("dp-coll-delete").DeleteContainerAsync();

        // Assert
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await db.Database.GetContainer("dp-coll-delete").ReadContainerAsync());
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Container_WhenReplaced_ReturnsUpdatedResource()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync("dp-coll-replace-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("dp-coll-replace", "/pk"));
        var container = db.Database.GetContainer("dp-coll-replace");

        var props = (await container.ReadContainerAsync()).Resource;
        props.DefaultTimeToLive = 3600;

        // Act
        var result = await container.ReplaceContainerAsync(props);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(result.Resource.Id, Is.EqualTo("dp-coll-replace"));
        Assert.That(result.Resource.DefaultTimeToLive, Is.EqualTo(3600));
    }

    [Test]
    public async Task Containers_WhenListed_ContainsCreatedContainers()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync("dp-coll-list-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("dp-coll-list-a", "/pk"));
        await db.Database.CreateContainerAsync(new ContainerProperties("dp-coll-list-b", "/pk"));

        // Act
        var iterator = db.Database.GetContainerQueryIterator<ContainerProperties>();
        var names = new List<string>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            foreach (var c in page)
                names.Add(c.Id);
        }

        // Assert
        Assert.That(names, Does.Contain("dp-coll-list-a"));
        Assert.That(names, Does.Contain("dp-coll-list-b"));
    }

    [Test]
    public async Task Container_CreatedViaArm_IsReadableViaDataPlane()
    {
        // Arrange — create via ARM SDK
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, AccountName, MinimalAccountContent());
        var account = accountResult.Value;

        var dbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlDatabaseResourceInfo("dp-arm2dp-db"));
        await account.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "dp-arm2dp-db", dbContent);

        var containerContent = new CosmosDBSqlContainerCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlContainerResourceInfo("dp-arm2dp-coll")
            {
                PartitionKey = new CosmosDBContainerPartitionKey { Paths = { "/pk" } }
            });
        await account.GetCosmosDBSqlDatabases().Get("dp-arm2dp-db").Value
            .GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "dp-arm2dp-coll", containerContent);

        // Act — read via Cosmos SDK data-plane
        var keys = await account.GetKeysAsync();
        using var cosmosClient = CreateCosmosClient(account.Data.DocumentEndpoint!, keys.Value.PrimaryMasterKey!);
        var result = await cosmosClient.GetDatabase("dp-arm2dp-db").GetContainer("dp-arm2dp-coll").ReadContainerAsync();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(result.Resource.Id, Is.EqualTo("dp-arm2dp-coll"));
    }

    [Test]
    public async Task Container_CreatedViaDataPlane_IsVisibleViaArm()
    {
        // Arrange — create database and container via Cosmos SDK data-plane
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var cosmosClient = CreateCosmosClient(endpoint, primaryKey);
        var db = await cosmosClient.CreateDatabaseAsync("dp-dp2arm-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("dp-dp2arm-coll", "/pk"));

        // Act — read via ARM SDK
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var account = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(AccountName);
        var sqlDb = await account.Value.GetCosmosDBSqlDatabases().GetAsync("dp-dp2arm-db");
        var container = await sqlDb.Value.GetCosmosDBSqlContainers().GetAsync("dp-dp2arm-coll");

        // Assert
        Assert.That(container.Value.Data.Name, Is.EqualTo("dp-dp2arm-coll"));
    }
}
