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

    // ─────────────────────────────────────────────────────────────────────────
    //  Document (Item) CRUD tests
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<Container> CreateContainerWithPartitionKey(string databaseName, string containerName)
    {
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        using var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync(databaseName);
        var result = await db.Database.CreateContainerAsync(
            new ContainerProperties(containerName, "/pk"));
        return result.Container;
    }

    private async Task<(string Endpoint, string PrimaryKey, Container Container)> PrepareContainer(
        string databaseName, string containerName)
    {
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        var client = CreateCosmosClient(endpoint, primaryKey);
        var db = await client.CreateDatabaseAsync(databaseName);
        var c = await db.Database.CreateContainerAsync(
            new ContainerProperties(containerName, "/pk"));
        return (endpoint, primaryKey, c.Container);
    }

    [Test]
    public async Task Item_WhenCreated_ReturnsCorrectId()
    {
        var (_, _, container) = await PrepareContainer("doc-create-db", "doc-create-coll");

        var item = new { id = "item-1", pk = "pk1", value = 42 };
        var result = await container.CreateItemAsync(item, new PartitionKey("pk1"));

        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
        Assert.That(result.Resource.id.ToString(), Is.EqualTo("item-1"));
    }

    [Test]
    public async Task Item_WhenRead_ReturnsItem()
    {
        var (_, _, container) = await PrepareContainer("doc-read-db", "doc-read-coll");

        var item = new { id = "item-read", pk = "pk-read", name = "hello" };
        await container.CreateItemAsync(item, new PartitionKey("pk-read"));

        var result = await container.ReadItemAsync<dynamic>("item-read", new PartitionKey("pk-read"));

        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(result.Resource.id.ToString(), Is.EqualTo("item-read"));
    }

    [Test]
    public async Task Item_WhenReplaced_ReturnsUpdatedFields()
    {
        var (_, _, container) = await PrepareContainer("doc-replace-db", "doc-replace-coll");

        var item = new { id = "item-replace", pk = "pk-replace", value = 1 };
        await container.CreateItemAsync(item, new PartitionKey("pk-replace"));

        var updated = new { id = "item-replace", pk = "pk-replace", value = 99 };
        var result = await container.ReplaceItemAsync(updated, "item-replace", new PartitionKey("pk-replace"));

        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That((int)result.Resource.value, Is.EqualTo(99));
    }

    [Test]
    public async Task Item_WhenPatched_UpdatesSpecificField()
    {
        var (_, _, container) = await PrepareContainer("doc-patch-db", "doc-patch-coll");

        var item = new { id = "item-patch", pk = "pk-patch", counter = 10 };
        await container.CreateItemAsync(item, new PartitionKey("pk-patch"));

        var patchOps = new[] { PatchOperation.Increment("/counter", 5) };
        var result = await container.PatchItemAsync<dynamic>("item-patch", new PartitionKey("pk-patch"), patchOps);

        Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That((double)result.Resource.counter, Is.EqualTo(15));
    }

    [Test]
    public async Task Item_WhenDeleted_ReturnsNotFound()
    {
        var (_, _, container) = await PrepareContainer("doc-delete-db", "doc-delete-coll");

        var item = new { id = "item-delete", pk = "pk-delete" };
        await container.CreateItemAsync(item, new PartitionKey("pk-delete"));

        await container.DeleteItemAsync<dynamic>("item-delete", new PartitionKey("pk-delete"));

        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await container.ReadItemAsync<dynamic>("item-delete", new PartitionKey("pk-delete")));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Items_WhenListed_ContainsCreatedItems()
    {
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();

        // Create database and container via ARM so the path is well-known
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var accountArm = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(AccountName);
        await accountArm.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "doc-list-db",
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope,
                    new CosmosDBSqlDatabaseResourceInfo("doc-list-db")));
        await accountArm.Value.GetCosmosDBSqlDatabases().Get("doc-list-db").Value
            .GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "doc-list-coll",
                new CosmosDBSqlContainerCreateOrUpdateContent(AzureLocation.WestEurope,
                    new CosmosDBSqlContainerResourceInfo("doc-list-coll")
                    {
                        PartitionKey = new CosmosDBContainerPartitionKey { Paths = { "/pk" } }
                    }));

        var cosmosClient = CreateCosmosClient(endpoint, primaryKey);
        var container = cosmosClient.GetDatabase("doc-list-db").GetContainer("doc-list-coll");

        await container.CreateItemAsync(new { id = "list-a", pk = "pk1" }, new PartitionKey("pk1"));
        await container.CreateItemAsync(new { id = "list-b", pk = "pk2" }, new PartitionKey("pk2"));

        // Call list endpoint directly — SDK doesn't expose a raw "list all" without a query
        using var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        var accountName = AccountName;
        var port = Topaz.Shared.GlobalSettings.DefaultCosmosDbPort;
        var dateStr = DateTimeOffset.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            System.Globalization.CultureInfo.InvariantCulture);

        // Build HMAC-SHA256 authorization for the list-docs request
        var resourceLink = "dbs/doc-list-db/colls/doc-list-coll";
        var resourceType = "docs";
        var stringToSign = $"get\n{resourceType}\n{resourceLink}\n{dateStr.ToLowerInvariant()}\n\n";
        var keyBytes = Convert.FromBase64String(primaryKey);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var sig = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stringToSign)));
        var authHeader = Uri.EscapeDataString($"type=master&ver=1.0&sig={sig}");

        var url = $"https://{accountName}.documents.topaz.local.dev:{port}/{resourceLink}/{resourceType}";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Authorization", authHeader);
        req.Headers.Add("x-ms-date", dateStr);
        req.Headers.Add("x-ms-version", "2018-12-31");

        var resp = await httpClient.SendAsync(req);
        Assert.That(resp.IsSuccessStatusCode, Is.True);

        var json = await resp.Content.ReadAsStringAsync();
        Assert.That(json, Does.Contain("list-a"));
        Assert.That(json, Does.Contain("list-b"));
    }

    [Test]
    public async Task Item_WithStaleETag_ReturnsPreconditionFailed()
    {
        var (_, _, container) = await PrepareContainer("doc-etag-db", "doc-etag-coll");

        var item = new { id = "item-etag", pk = "pk-etag", value = 1 };
        await container.CreateItemAsync(item, new PartitionKey("pk-etag"));

        var staleEtag = "\"00000000000000000000000000000000\"";
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await container.ReplaceItemAsync(
                new { id = "item-etag", pk = "pk-etag", value = 2 },
                "item-etag",
                new PartitionKey("pk-etag"),
                new ItemRequestOptions { IfMatchEtag = staleEtag }));

        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.PreconditionFailed));
    }

    [Test]
    public async Task Item_CreatedTwice_ReturnsConflict()
    {
        var (_, _, container) = await PrepareContainer("doc-conflict-db", "doc-conflict-coll");

        var item = new { id = "item-conflict", pk = "pk-conflict" };
        await container.CreateItemAsync(item, new PartitionKey("pk-conflict"));

        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await container.CreateItemAsync(item, new PartitionKey("pk-conflict")));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Conflict));
    }
}
