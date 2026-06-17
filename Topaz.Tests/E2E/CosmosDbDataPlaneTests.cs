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
}
