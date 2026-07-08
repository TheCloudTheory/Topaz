using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

/// <summary>
/// Verifies that Cosmos DB data-plane endpoints enforce the bearer-authentication
/// change introduced alongside <c>CosmosDbDataPlaneAuthorizationChecker</c>:
///
/// <list type="bullet">
///   <item>The global-admin bearer token is always accepted.</item>
///   <item>Any other bearer token is rejected (the endpoints declare no required
///         permissions, and <c>HasAnyRequiredPermission</c> returns false for an
///         empty required set, so only GlobalAdminId bypasses the check).</item>
///   <item>Master-key (shared-key) authentication continues to work when local
///         auth is enabled.</item>
///   <item>When <c>DisableLocalAuth = true</c>, master-key auth is rejected and
///         the GlobalAdmin bearer is still accepted.</item>
/// </list>
/// </summary>
public class CosmosDbDataPlaneAuthorizationTests
{
    private readonly List<CosmosClient> _ownedClients = [];
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("C7D3F1A2-B4E5-4C6D-9A8B-AA0000BBCC01");

    private const string SubscriptionName = "sub-test-cosmosdb-dp-auth";
    private const string ResourceGroupName = "rg-test-cosmosdb-dp-auth";
    private const string AccountName = "cosmos-dp-auth-test";

    [TearDown]
    public void TearDown()
    {
        foreach (var c in _ownedClients)
            c.Dispose();
        _ownedClients.Clear();
    }

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    // -------------------------------------------------------------------------
    // Bearer auth — GlobalAdmin
    // -------------------------------------------------------------------------

    [Test]
    public async Task BearerAuth_GlobalAdmin_CanCreateDatabase()
    {
        // Arrange
        var endpoint = await CreateAccountAndGetEndpoint(AccountName);
        using var client = CreateCosmosClientWithBearer(endpoint, Globals.GlobalAdminId);

        // Act
        var result = await client.CreateDatabaseAsync("auth-bearer-create-db");

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
    }

    [Test]
    public async Task BearerAuth_GlobalAdmin_CanReadDatabase()
    {
        // Arrange
        var endpoint = await CreateAccountAndGetEndpoint(AccountName);
        var (_, primaryKey) = await GetAccountCredentials(AccountName);
        using var masterKeyClient = CreateCosmosClientWithMasterKey(endpoint, primaryKey);
        await masterKeyClient.CreateDatabaseAsync("auth-bearer-read-db");

        using var bearerClient = CreateCosmosClientWithBearer(endpoint, Globals.GlobalAdminId);

        // Act
        var result = await bearerClient.GetDatabase("auth-bearer-read-db").ReadAsync();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(result.Resource.Id, Is.EqualTo("auth-bearer-read-db"));
    }

    [Test]
    public async Task BearerAuth_GlobalAdmin_CanListDatabases()
    {
        // Arrange
        var endpoint = await CreateAccountAndGetEndpoint(AccountName);
        var (_, primaryKey) = await GetAccountCredentials(AccountName);
        using var masterKeyClient = CreateCosmosClientWithMasterKey(endpoint, primaryKey);
        await masterKeyClient.CreateDatabaseAsync("auth-bearer-list-a");
        await masterKeyClient.CreateDatabaseAsync("auth-bearer-list-b");

        using var bearerClient = CreateCosmosClientWithBearer(endpoint, Globals.GlobalAdminId);

        // Act
        var iterator = bearerClient.GetDatabaseQueryIterator<DatabaseProperties>();
        var names = new List<string>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            foreach (var db in page)
                names.Add(db.Id);
        }

        // Assert
        Assert.That(names, Does.Contain("auth-bearer-list-a"));
        Assert.That(names, Does.Contain("auth-bearer-list-b"));
    }

    // -------------------------------------------------------------------------
    // Bearer auth — non-admin principal (no role assignment)
    // -------------------------------------------------------------------------

    [Test]
    public async Task BearerAuth_NonAdminPrincipal_CreateDatabase_IsRejectedWith401()
    {
        // Arrange: any random principal ID that is not the GlobalAdminId
        var nonAdminPrincipalId = Guid.NewGuid().ToString();
        var endpoint = await CreateAccountAndGetEndpoint(AccountName);
        using var client = CreateCosmosClientWithBearer(endpoint, nonAdminPrincipalId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.CreateDatabaseAsync("should-be-rejected"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task BearerAuth_NonAdminPrincipal_ReadDatabase_IsRejectedWith401()
    {
        // Arrange
        var nonAdminPrincipalId = Guid.NewGuid().ToString();
        var endpoint = await CreateAccountAndGetEndpoint(AccountName);
        var (_, primaryKey) = await GetAccountCredentials(AccountName);
        using var masterKeyClient = CreateCosmosClientWithMasterKey(endpoint, primaryKey);
        await masterKeyClient.CreateDatabaseAsync("non-admin-read-db");

        using var bearerClient = CreateCosmosClientWithBearer(endpoint, nonAdminPrincipalId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await bearerClient.GetDatabase("non-admin-read-db").ReadAsync());
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    // -------------------------------------------------------------------------
    // Master-key auth — still works
    // -------------------------------------------------------------------------

    [Test]
    public async Task MasterKeyAuth_CanCreateDatabase_WhenLocalAuthEnabled()
    {
        // Arrange
        var (endpoint, primaryKey) = await CreateAccountWithCredentials(AccountName);
        using var client = CreateCosmosClientWithMasterKey(endpoint, primaryKey);

        // Act
        var result = await client.CreateDatabaseAsync("masterkey-create-db");

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
    }

    // -------------------------------------------------------------------------
    // DisableLocalAuth
    // -------------------------------------------------------------------------

    [Test]
    public async Task DisableLocalAuth_MasterKey_IsRejectedWith401()
    {
        // Arrange
        const string accountName = "cosmos-dp-auth-no-local";
        var content = MinimalAccountContent();
        content.DisableLocalAuth = true;

        var (endpoint, primaryKey) = await CreateAccountWithContent(accountName, content);
        using var client = CreateCosmosClientWithMasterKey(endpoint, primaryKey);

        // Act & Assert
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.CreateDatabaseAsync("should-be-rejected-local"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DisableLocalAuth_BearerGlobalAdmin_CanCreateDatabase()
    {
        // Arrange
        const string accountName = "cosmos-dp-auth-no-local-bearer";
        var content = MinimalAccountContent();
        content.DisableLocalAuth = true;

        var endpoint = await CreateAccountAndGetEndpoint(accountName, content);
        using var client = CreateCosmosClientWithBearer(endpoint, Globals.GlobalAdminId);

        // Act
        var result = await client.CreateDatabaseAsync("bearer-no-local-auth-db");

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
    }

    [Test]
    public async Task DisableLocalAuth_BearerNonAdmin_IsRejectedWith401()
    {
        // Arrange
        const string accountName = "cosmos-dp-auth-no-local-nonadmin";
        var nonAdminPrincipalId = Guid.NewGuid().ToString();
        var content = MinimalAccountContent();
        content.DisableLocalAuth = true;

        var endpoint = await CreateAccountAndGetEndpoint(accountName, content);
        using var client = CreateCosmosClientWithBearer(endpoint, nonAdminPrincipalId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.CreateDatabaseAsync("should-fail"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static CosmosDBAccountCreateOrUpdateContent MinimalAccountContent() =>
        new(AzureLocation.WestEurope, [new CosmosDBAccountLocation { LocationName = "westeurope" }]);

    private async Task<string> CreateAccountAndGetEndpoint(
        string accountName,
        CosmosDBAccountCreateOrUpdateContent? content = null)
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, content ?? MinimalAccountContent());
        return accountResult.Value.Data.DocumentEndpoint!;
    }

    private async Task<(string Endpoint, string PrimaryKey)> GetAccountCredentials(string accountName)
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var account = await resourceGroup.Value.GetCosmosDBAccountAsync(accountName);
        var keys = await account.Value.GetKeysAsync();
        return (account.Value.Data.DocumentEndpoint!, keys.Value.PrimaryMasterKey!);
    }

    private async Task<(string Endpoint, string PrimaryKey)> CreateAccountWithCredentials(string accountName)
    {
        var endpoint = await CreateAccountAndGetEndpoint(accountName);
        var (_, primaryKey) = await GetAccountCredentials(accountName);
        return (endpoint, primaryKey);
    }

    private async Task<(string Endpoint, string PrimaryKey)> CreateAccountWithContent(
        string accountName,
        CosmosDBAccountCreateOrUpdateContent content)
    {
        var endpoint = await CreateAccountAndGetEndpoint(accountName, content);
        var (_, primaryKey) = await GetAccountCredentials(accountName);
        return (endpoint, primaryKey);
    }

    private CosmosClient CreateCosmosClientWithMasterKey(string endpoint, string primaryKey)
    {
        var client = new CosmosClient(endpoint, primaryKey, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler())
        });
        _ownedClients.Add(client);
        return client;
    }

    private CosmosClient CreateCosmosClientWithBearer(string endpoint, string principalId)
    {
        var client = new CosmosClient(endpoint, new AzureLocalCredential(principalId), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler())
        });
        _ownedClients.Add(client);
        return client;
    }
}
