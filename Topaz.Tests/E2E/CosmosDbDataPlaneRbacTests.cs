using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

/// <summary>
/// Verifies that Cosmos DB data-plane RBAC is enforced at three scopes:
///
/// <list type="bullet">
///   <item><b>Account scope</b> — a role assignment at the account resource grants access
///     to all databases, containers, and documents within that account, but not in a
///     different account.</item>
///   <item><b>Database scope</b> — a role assignment scoped to a specific database grants
///     access to containers/documents in that database only, not in a sibling database.</item>
///   <item><b>Container scope</b> — a role assignment scoped to a specific container grants
///     access to documents in that container only, not in a sibling container.</item>
/// </list>
///
/// Each test uses <see cref="AzureLocalCredential"/> with a random principal ID so that
/// no pre-existing grants can bleed across tests.
/// </summary>
public class CosmosDbDataPlaneRbacTests
{
    private readonly List<CosmosClient> _ownedClients = [];

    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D1E2F3A4-B5C6-4D7E-8F9A-BB0000CC0100");

    private const string SubscriptionName = "sub-test-cosmosdb-rbac";
    private const string ResourceGroupName = "rg-test-cosmosdb-rbac";

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
    // Account-scope RBAC
    // -------------------------------------------------------------------------

    [Test]
    public async Task AccountScope_PrincipalWithRole_CanCreateAndReadDatabase()
    {
        var principalId = Guid.NewGuid();
        var (accountName, endpoint) = await CreateAccount("rbac-acct-scope-a");

        await GrantRoleAtAccountScope(accountName, principalId,
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/write",
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/read");

        using var client = CreateBearerClient(endpoint, principalId);

        var created = await client.CreateDatabaseAsync("acct-scope-db");
        Assert.That(created.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        var read = await client.GetDatabase("acct-scope-db").ReadAsync();
        Assert.That(read.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
    }

    [Test]
    public async Task AccountScope_PrincipalWithRole_CannotAccessDifferentAccount()
    {
        var principalId = Guid.NewGuid();
        var (accountNameA, endpointA) = await CreateAccount("rbac-acct-a");
        var (_, endpointB) = await CreateAccount("rbac-acct-b");

        // Grant only on account A
        await GrantRoleAtAccountScope(accountNameA, principalId,
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/write");

        using var clientA = CreateBearerClient(endpointA, principalId);
        using var clientB = CreateBearerClient(endpointB, principalId);

        // Can create in account A
        var ok = await clientA.CreateDatabaseAsync("allowed-db");
        Assert.That(ok.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        // Cannot create in account B
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await clientB.CreateDatabaseAsync("denied-db"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AccountScope_PrincipalWithRole_CanCreateContainer()
    {
        var principalId = Guid.NewGuid();
        var (accountName, endpoint) = await CreateAccount("rbac-acct-coll");

        await GrantRoleAtAccountScope(accountName, principalId,
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/write",
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/write");

        using var client = CreateBearerClient(endpoint, principalId);
        var db = await client.CreateDatabaseAsync("acct-coll-db");
        var result = await db.Database.CreateContainerAsync(new ContainerProperties("my-coll", "/pk"));
        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
    }

    // -------------------------------------------------------------------------
    // Database-scope RBAC
    // -------------------------------------------------------------------------

    [Test]
    public async Task DatabaseScope_PrincipalWithRole_CanAccessTargetDatabase()
    {
        var principalId = Guid.NewGuid();
        var (accountName, endpoint) = await CreateAccount("rbac-db-scope");

        // Pre-create both databases via master key
        var (_, primaryKey) = await GetAccountCredentials(accountName);
        using var adminClient = CreateMasterKeyClient(endpoint, primaryKey);
        await adminClient.CreateDatabaseAsync("db-allowed");
        await adminClient.CreateDatabaseAsync("db-denied");

        // Grant principal access only to db-allowed
        await GrantRoleAtDatabaseScope(accountName, "db-allowed", principalId,
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/write",
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/read");

        using var client = CreateBearerClient(endpoint, principalId);

        // Can create container in db-allowed
        var ok = await client.GetDatabase("db-allowed")
            .CreateContainerAsync(new ContainerProperties("coll-ok", "/pk"));
        Assert.That(ok.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        // Cannot create container in db-denied
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.GetDatabase("db-denied")
                .CreateContainerAsync(new ContainerProperties("coll-denied", "/pk")));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DatabaseScope_PrincipalWithRole_CanCreateAndReadDocument()
    {
        var principalId = Guid.NewGuid();
        var (accountName, endpoint) = await CreateAccount("rbac-db-docs");

        var (_, primaryKey) = await GetAccountCredentials(accountName);
        using var adminClient = CreateMasterKeyClient(endpoint, primaryKey);
        var db = await adminClient.CreateDatabaseAsync("docs-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("docs-coll", "/pk"));

        await GrantRoleAtDatabaseScope(accountName, "docs-db", principalId,
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/write",
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read");

        using var client = CreateBearerClient(endpoint, principalId);
        var container = client.GetDatabase("docs-db").GetContainer("docs-coll");

        var doc = new { id = "doc1", pk = "p1", value = "hello" };
        var created = await container.CreateItemAsync(doc, new PartitionKey("p1"));
        Assert.That(created.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        var read = await container.ReadItemAsync<dynamic>("doc1", new PartitionKey("p1"));
        Assert.That(read.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
    }

    // -------------------------------------------------------------------------
    // Container-scope RBAC
    // -------------------------------------------------------------------------

    [Test]
    public async Task ContainerScope_PrincipalWithRole_CanAccessTargetContainer()
    {
        var principalId = Guid.NewGuid();
        var (accountName, endpoint) = await CreateAccount("rbac-coll-scope");

        var (_, primaryKey) = await GetAccountCredentials(accountName);
        using var adminClient = CreateMasterKeyClient(endpoint, primaryKey);
        var db = await adminClient.CreateDatabaseAsync("coll-scope-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("coll-allowed", "/pk"));
        await db.Database.CreateContainerAsync(new ContainerProperties("coll-denied", "/pk"));

        // Grant only on coll-allowed
        await GrantRoleAtContainerScope(accountName, "coll-scope-db", "coll-allowed", principalId,
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/write",
            "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read");

        using var client = CreateBearerClient(endpoint, principalId);

        // Can write document to coll-allowed
        var ok = await client.GetDatabase("coll-scope-db")
            .GetContainer("coll-allowed")
            .CreateItemAsync(new { id = "d1", pk = "p" }, new PartitionKey("p"));
        Assert.That(ok.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        // Cannot write document to coll-denied
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.GetDatabase("coll-scope-db")
                .GetContainer("coll-denied")
                .CreateItemAsync(new { id = "d2", pk = "p" }, new PartitionKey("p")));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ContainerScope_PrincipalWithoutRole_IsRejected()
    {
        var principalId = Guid.NewGuid();
        var (accountName, endpoint) = await CreateAccount("rbac-no-coll-role");

        var (_, primaryKey) = await GetAccountCredentials(accountName);
        using var adminClient = CreateMasterKeyClient(endpoint, primaryKey);
        var db = await adminClient.CreateDatabaseAsync("no-role-db");
        await db.Database.CreateContainerAsync(new ContainerProperties("no-role-coll", "/pk"));

        // No role assignment at all
        using var client = CreateBearerClient(endpoint, principalId);

        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await client.GetDatabase("no-role-db")
                .GetContainer("no-role-coll")
                .CreateItemAsync(new { id = "d1", pk = "p" }, new PartitionKey("p")));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private async Task<(string AccountName, string Endpoint)> CreateAccount(string name)
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var content = new CosmosDBAccountCreateOrUpdateContent(
            AzureLocation.WestEurope, [new CosmosDBAccountLocation { LocationName = "westeurope" }]);
        var result = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, name, content);
        return (name, result.Value.Data.DocumentEndpoint!);
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

    private async Task GrantRoleAtAccountScope(string accountName, Guid principalId, params string[] actions)
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefId = await CreateRoleDefinition(subscription, actions);
        var account = await (await subscription.GetResourceGroupAsync(ResourceGroupName))
            .Value.GetCosmosDBAccountAsync(accountName);
        await account.Value.GetAuthorizationRoleAssignments()
            .CreateOrUpdateAsync(WaitUntil.Completed,
                new ResourceIdentifier(Guid.NewGuid().ToString()),
                new RoleAssignmentCreateOrUpdateContent(roleDefId, principalId));
    }

    private async Task GrantRoleAtDatabaseScope(
        string accountName, string databaseName, Guid principalId, params string[] actions)
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefId = await CreateRoleDefinition(subscription, actions);

        var dbScope = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}" +
            $"/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}");
        var collection = armClient.GetAuthorizationRoleAssignments(dbScope);
        await collection.CreateOrUpdateAsync(WaitUntil.Completed,
            new ResourceIdentifier(Guid.NewGuid().ToString()),
            new RoleAssignmentCreateOrUpdateContent(roleDefId, principalId));
    }

    private async Task GrantRoleAtContainerScope(
        string accountName, string databaseName, string containerName, Guid principalId, params string[] actions)
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefId = await CreateRoleDefinition(subscription, actions);

        var containerScope = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}" +
            $"/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}" +
            $"/sqlDatabases/{databaseName}/containers/{containerName}");
        var collection = armClient.GetAuthorizationRoleAssignments(containerScope);
        await collection.CreateOrUpdateAsync(WaitUntil.Completed,
            new ResourceIdentifier(Guid.NewGuid().ToString()),
            new RoleAssignmentCreateOrUpdateContent(roleDefId, principalId));
    }

    private async Task<ResourceIdentifier> CreateRoleDefinition(
        SubscriptionResource subscription, string[] actions)
    {
        var roleDefId = new ResourceIdentifier(Guid.NewGuid().ToString());
        var roleDefData = new AuthorizationRoleDefinitionData
        {
            RoleName = $"cosmos-test-role-{Guid.NewGuid():N}",
            Description = "Topaz test role for Cosmos DB RBAC"
        };
        roleDefData.Permissions.Add(new RoleDefinitionPermission { Actions = { } });
        foreach (var a in actions)
            roleDefData.Permissions[0].Actions.Add(a);
        roleDefData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        await subscription.GetAuthorizationRoleDefinitions()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleDefId, roleDefData);
        return roleDefId;
    }

    private CosmosClient CreateBearerClient(string endpoint, Guid principalId)
    {
        var client = new CosmosClient(endpoint, new AzureLocalCredential(principalId.ToString()),
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
                HttpClientFactory = () => new HttpClient(new HttpClientHandler())
            });
        _ownedClients.Add(client);
        return client;
    }

    private CosmosClient CreateMasterKeyClient(string endpoint, string primaryKey)
    {
        var client = new CosmosClient(endpoint, primaryKey,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
                HttpClientFactory = () => new HttpClient(new HttpClientHandler())
            });
        _ownedClients.Add(client);
        return client;
    }
}
