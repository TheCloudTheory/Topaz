using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Topaz.CLI;
using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.CosmosDb;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class CosmosDbDataPlaneTests
{
    private readonly List<CosmosClient> _ownedClients = [];
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003300");

    private const string SubscriptionName = "sub-test-cosmosdb-dp";
    private const string ResourceGroupName = "rg-test-cosmosdb-dp";
    private const string AccountName = "test-cosmos-dp";

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
            LimitToEndpoint = true,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler())
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
            Assert.That(result.Resource.Id, Is.EqualTo("dp-coll-create"));
        }
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(result.Resource.Id, Is.EqualTo("dp-coll-read"));
        }
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(result.Resource.Id, Is.EqualTo("dp-coll-replace"));
            Assert.That(result.Resource.DefaultTimeToLive, Is.EqualTo(3600));
        }
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
        _ownedClients.Add(client);
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
        _ownedClients.Add(cosmosClient);
        var container = cosmosClient.GetDatabase("doc-list-db").GetContainer("doc-list-coll");

        await container.CreateItemAsync(new { id = "list-a", pk = "pk1" }, new PartitionKey("pk1"));
        await container.CreateItemAsync(new { id = "list-b", pk = "pk2" }, new PartitionKey("pk2"));

        // Call list endpoint directly — SDK doesn't expose a raw "list all" without a query
        using var httpClient = new HttpClient();

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

    // ─────────────────────────────────────────────────────────────────────────
    //  SQL query tests
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(string Endpoint, string PrimaryKey)> SeedQueryContainer(
        string databaseName, string containerName, IEnumerable<object> items)
    {
        var (endpoint, primaryKey) = await CreateAccountAndGetCredentials();
        var client = CreateCosmosClient(endpoint, primaryKey);
        try
        {
            var db = await client.CreateDatabaseAsync(databaseName);
            var c = (await db.Database.CreateContainerAsync(
                new ContainerProperties(containerName, "/pk"))).Container;
            foreach (var item in items)
                await c.CreateItemAsync(item, new PartitionKey(((dynamic)item).pk.ToString()));
        }
        finally
        {
            client.Dispose();
        }
        return (endpoint, primaryKey);
    }

    private static async Task<System.Text.Json.Nodes.JsonObject> ExecuteQueryAsync(
        string primaryKey, string database, string collection,
        string sql, object[]? parameters = null,
        int? maxItemCount = null, string? continuation = null)
    {
        var port = Topaz.Shared.GlobalSettings.DefaultCosmosDbPort;
        var endpoint = $"https://{AccountName}.documents.topaz.local.dev:{port}";
        using var cosmosClient = CreateCosmosClient(endpoint, primaryKey);

        var queryDef = new QueryDefinition(sql);
        if (parameters != null)
            foreach (dynamic p in parameters)
                queryDef = queryDef.WithParameter((string)p.name, (object)p.value);

        var options = new QueryRequestOptions();
        if (maxItemCount.HasValue)
            options.MaxItemCount = maxItemCount.Value;

        var iterator = cosmosClient
            .GetDatabase(database)
            .GetContainer(collection)
            .GetItemQueryIterator<Newtonsoft.Json.Linq.JToken>(queryDef, continuation, options);

        var page = await iterator.ReadNextAsync();

        var docs = new System.Text.Json.Nodes.JsonArray();
        foreach (var item in page)
        {
            if (item == null)
                docs.Add(null);
            else
            {
                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(item.ToString(Newtonsoft.Json.Formatting.None));
                docs.Add(jsonNode);
            }
        }

        return new System.Text.Json.Nodes.JsonObject
        {
            ["Documents"] = docs,
            ["_count"] = docs.Count,
            ["_continuation"] = page.ContinuationToken
        };
    }

    [Test]
    public async Task Query_SelectStar_ReturnsAllDocuments()
    {
        var (_, key) = await SeedQueryContainer("q-star-db", "q-star-coll",
        [
            new { id = "q1", pk = "p1", name = "Alice" },
            new { id = "q2", pk = "p2", name = "Bob" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-star-db", "q-star-coll", "SELECT * FROM c");
        Assert.That((int)result["_count"]!, Is.EqualTo(2));
    }

    [Test]
    public async Task Query_WhereEquality_FiltersCorrectly()
    {
        var (_, key) = await SeedQueryContainer("q-eq-db", "q-eq-coll",
        [
            new { id = "e1", pk = "p1", category = "A" },
            new { id = "e2", pk = "p2", category = "B" },
            new { id = "e3", pk = "p3", category = "A" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-eq-db", "q-eq-coll",
            "SELECT * FROM c WHERE c.category = 'A'");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(2));
        Assert.That(docs.All(d => d!["category"]!.GetValue<string>() == "A"), Is.True);
    }

    [Test]
    public async Task Query_WhereGreaterThan_FiltersCorrectly()
    {
        var (_, key) = await SeedQueryContainer("q-gt-db", "q-gt-coll",
        [
            new { id = "g1", pk = "p1", age = 20 },
            new { id = "g2", pk = "p2", age = 30 },
            new { id = "g3", pk = "p3", age = 40 }
        ]);

        var result = await ExecuteQueryAsync(key, "q-gt-db", "q-gt-coll",
            "SELECT * FROM c WHERE c.age > 25");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(2));
        Assert.That(docs.All(d => d!["age"]!.GetValue<double>() > 25), Is.True);
    }

    [Test]
    public async Task Query_FieldProjection_ReturnsOnlyRequestedFields()
    {
        var (_, key) = await SeedQueryContainer("q-proj-db", "q-proj-coll",
        [
            new { id = "p1", pk = "x", name = "Alice", secret = "hidden" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-proj-db", "q-proj-coll",
            "SELECT c.name FROM c");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(1));
        Assert.That(docs[0]!.ToJsonString(), Does.Contain("name"));
        Assert.That(docs[0]!.ToJsonString(), Does.Not.Contain("secret"));
    }

    [Test]
    public async Task Query_ValueProjection_ReturnsScalarArray()
    {
        var (_, key) = await SeedQueryContainer("q-val-db", "q-val-coll",
        [
            new { id = "v1", pk = "p1", name = "Alice" },
            new { id = "v2", pk = "p2", name = "Bob" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-val-db", "q-val-coll",
            "SELECT VALUE c.name FROM c");
        var names = result["Documents"]!.AsArray().Select(d => d!.GetValue<string>()).ToList();
        Assert.That(names, Does.Contain("Alice"));
        Assert.That(names, Does.Contain("Bob"));
    }

    [Test]
    public async Task Query_OrderByAsc_ReturnsSortedResults()
    {
        var (_, key) = await SeedQueryContainer("q-asc-db", "q-asc-coll",
        [
            new { id = "s1", pk = "p1", score = 30 },
            new { id = "s2", pk = "p2", score = 10 },
            new { id = "s3", pk = "p3", score = 20 }
        ]);

        var result = await ExecuteQueryAsync(key, "q-asc-db", "q-asc-coll",
            "SELECT * FROM c ORDER BY c.score ASC");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(3));
        Assert.That(docs[0]!["score"]!.GetValue<double>(), Is.EqualTo(10));
        Assert.That(docs[1]!["score"]!.GetValue<double>(), Is.EqualTo(20));
        Assert.That(docs[2]!["score"]!.GetValue<double>(), Is.EqualTo(30));
    }

    [Test]
    public async Task Query_OrderByDesc_ReturnsSortedResults()
    {
        var (_, key) = await SeedQueryContainer("q-desc-db", "q-desc-coll",
        [
            new { id = "d1", pk = "p1", score = 10 },
            new { id = "d2", pk = "p2", score = 30 },
            new { id = "d3", pk = "p3", score = 20 }
        ]);

        var result = await ExecuteQueryAsync(key, "q-desc-db", "q-desc-coll",
            "SELECT * FROM c ORDER BY c.score DESC");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(3));
        Assert.That(docs[0]!["score"]!.GetValue<double>(), Is.EqualTo(30));
        Assert.That(docs[1]!["score"]!.GetValue<double>(), Is.EqualTo(20));
        Assert.That(docs[2]!["score"]!.GetValue<double>(), Is.EqualTo(10));
    }

    [Test]
    public async Task Query_ParameterizedQuery_FiltersCorrectly()
    {
        var (_, key) = await SeedQueryContainer("q-param-db", "q-param-coll",
        [
            new { id = "pr1", pk = "p1", name = "Alice" },
            new { id = "pr2", pk = "p2", name = "Bob" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-param-db", "q-param-coll",
            "SELECT * FROM c WHERE c.name = @name",
            parameters: [new { name = "@name", value = "Alice" }]);
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(1));
        Assert.That(docs[0]!["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
    }

    [Test]
    public async Task Query_CountAggregate_ReturnsCorrectCount()
    {
        var (_, key) = await SeedQueryContainer("q-count-db", "q-count-coll",
        [
            new { id = "c1", pk = "p1" },
            new { id = "c2", pk = "p2" },
            new { id = "c3", pk = "p3" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-count-db", "q-count-coll",
            "SELECT VALUE COUNT(1) FROM c");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(1));
        Assert.That(docs[0]!.GetValue<long>(), Is.EqualTo(3));
    }

    [Test]
    public async Task Query_InOperator_FiltersCorrectly()
    {
        var (_, key) = await SeedQueryContainer("q-in-db", "q-in-coll",
        [
            new { id = "i1", pk = "p1", status = "active" },
            new { id = "i2", pk = "p2", status = "pending" },
            new { id = "i3", pk = "p3", status = "deleted" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-in-db", "q-in-coll",
            "SELECT * FROM c WHERE c.status IN ('active', 'pending')");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(2));
        Assert.That(docs.All(d => d!["status"]!.GetValue<string>() != "deleted"), Is.True);
    }

    [Test]
    public async Task Query_IsDefinedOperator_FiltersCorrectly()
    {
        var (_, key) = await SeedQueryContainer("q-def-db", "q-def-coll",
        [
            new { id = "d1", pk = "p1", optional = "present" },
            new { id = "d2", pk = "p2" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-def-db", "q-def-coll",
            "SELECT * FROM c WHERE IS_DEFINED(c.optional)");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(1));
        Assert.That(docs[0]!["id"]!.GetValue<string>(), Is.EqualTo("d1"));
    }

    [Test]
    public async Task Query_PaginationWithContinuationToken_ReturnsAllDocumentsAcrossPages()
    {
        var items = Enumerable.Range(1, 20)
            .Select(i => (object)new { id = $"page-{i:D2}", pk = $"p{i}" })
            .ToArray();
        var (_, key) = await SeedQueryContainer("q-page-db", "q-page-coll", items);

        var allDocs = new List<System.Text.Json.Nodes.JsonNode?>();
        string? continuation = null;
        var pageCount = 0;
        do
        {
            var result = await ExecuteQueryAsync(key, "q-page-db", "q-page-coll",
                "SELECT * FROM c", maxItemCount: 5, continuation: continuation);
            allDocs.AddRange(result["Documents"]!.AsArray());
            continuation = result["_continuation"]?.GetValue<string>();
            pageCount++;
        } while (continuation != null);

        Assert.That(allDocs.Count, Is.EqualTo(20));
        Assert.That(pageCount, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public async Task Query_GroupBy_Count_ReturnsOneRowPerGroup()
    {
        var (_, key) = await SeedQueryContainer("q-grp-cnt-db", "q-grp-cnt-coll",
        [
            new { id = "g1", pk = "p1", category = "A" },
            new { id = "g2", pk = "p2", category = "B" },
            new { id = "g3", pk = "p3", category = "A" },
            new { id = "g4", pk = "p4", category = "B" },
            new { id = "g5", pk = "p5", category = "C" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-cnt-db", "q-grp-cnt-coll",
            "SELECT c.category, COUNT(1) AS cnt FROM c GROUP BY c.category");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(3));

        var byCategory = docs
            .Select(d => (d!["category"]!.GetValue<string>(), (int)d["cnt"]!.GetValue<double>()))
            .OrderBy(t => t.Item1)
            .ToArray();
        Assert.That(byCategory[0], Is.EqualTo(("A", 2)));
        Assert.That(byCategory[1], Is.EqualTo(("B", 2)));
        Assert.That(byCategory[2], Is.EqualTo(("C", 1)));
    }

    [Test]
    public async Task Query_GroupBy_SumAndAvg_ReturnsCorrectAggregates()
    {
        var (_, key) = await SeedQueryContainer("q-grp-sum-db", "q-grp-sum-coll",
        [
            new { id = "s1", pk = "p1", category = "X", amount = 10 },
            new { id = "s2", pk = "p2", category = "X", amount = 20 },
            new { id = "s3", pk = "p3", category = "Y", amount = 5  }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-sum-db", "q-grp-sum-coll",
            "SELECT c.category, SUM(c.amount) AS total, AVG(c.amount) AS avg FROM c GROUP BY c.category");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(2));

        var x = docs.First(d => d!["category"]!.GetValue<string>() == "X")!;
        Assert.That(x["total"]!.GetValue<double>(), Is.EqualTo(30));
        Assert.That(x["avg"]!.GetValue<double>(), Is.EqualTo(15));

        var y = docs.First(d => d!["category"]!.GetValue<string>() == "Y")!;
        Assert.That(y["total"]!.GetValue<double>(), Is.EqualTo(5));
    }

    [Test]
    public async Task Query_GroupBy_MinMax_ReturnsCorrectAggregates()
    {
        var (_, key) = await SeedQueryContainer("q-grp-mm-db", "q-grp-mm-coll",
        [
            new { id = "m1", pk = "p1", category = "A", value = 3 },
            new { id = "m2", pk = "p2", category = "A", value = 7 },
            new { id = "m3", pk = "p3", category = "A", value = 5 }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-mm-db", "q-grp-mm-coll",
            "SELECT c.category, MIN(c.value) AS lo, MAX(c.value) AS hi FROM c GROUP BY c.category");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(1));
        Assert.That(docs[0]!["lo"]!.GetValue<double>(), Is.EqualTo(3));
        Assert.That(docs[0]!["hi"]!.GetValue<double>(), Is.EqualTo(7));
    }

    [Test]
    public async Task Query_GroupBy_EmptyCollection_ReturnsEmpty()
    {
        var (_, key) = await SeedQueryContainer("q-grp-empty-db", "q-grp-empty-coll", []);

        var result = await ExecuteQueryAsync(key, "q-grp-empty-db", "q-grp-empty-coll",
            "SELECT c.category, COUNT(1) AS cnt FROM c GROUP BY c.category");
        var docs = result["Documents"]!.AsArray();
        Assert.That(docs.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task Query_GroupBy_OrderByAggregateAscending_ReturnsSortedResults()
    {
        var (_, key) = await SeedQueryContainer("q-grp-oba-db", "q-grp-oba-coll",
        [
            new { id = "o1", pk = "p1", category = "B" },
            new { id = "o2", pk = "p2", category = "A" },
            new { id = "o3", pk = "p3", category = "A" },
            new { id = "o4", pk = "p4", category = "C" },
            new { id = "o5", pk = "p5", category = "C" },
            new { id = "o6", pk = "p6", category = "C" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-oba-db", "q-grp-oba-coll",
            "SELECT c.category, COUNT(1) AS cnt FROM c GROUP BY c.category ORDER BY cnt ASC");
        var docs = result["Documents"]!.AsArray();

        Assert.That(docs.Count, Is.EqualTo(3));
        Assert.That(docs[0]!["cnt"]!.GetValue<double>(), Is.EqualTo(1)); // B
        Assert.That(docs[1]!["cnt"]!.GetValue<double>(), Is.EqualTo(2)); // A
        Assert.That(docs[2]!["cnt"]!.GetValue<double>(), Is.EqualTo(3)); // C
    }

    [Test]
    public async Task Query_GroupBy_OrderByAggregateDescending_ReturnsSortedResults()
    {
        var (_, key) = await SeedQueryContainer("q-grp-obd-db", "q-grp-obd-coll",
        [
            new { id = "d1", pk = "p1", category = "B" },
            new { id = "d2", pk = "p2", category = "A" },
            new { id = "d3", pk = "p3", category = "A" },
            new { id = "d4", pk = "p4", category = "C" },
            new { id = "d5", pk = "p5", category = "C" },
            new { id = "d6", pk = "p6", category = "C" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-obd-db", "q-grp-obd-coll",
            "SELECT c.category, COUNT(1) AS cnt FROM c GROUP BY c.category ORDER BY cnt DESC");
        var docs = result["Documents"]!.AsArray();

        Assert.That(docs.Count, Is.EqualTo(3));
        Assert.That(docs[0]!["cnt"]!.GetValue<double>(), Is.EqualTo(3)); // C
        Assert.That(docs[1]!["cnt"]!.GetValue<double>(), Is.EqualTo(2)); // A
        Assert.That(docs[2]!["cnt"]!.GetValue<double>(), Is.EqualTo(1)); // B
    }

    [Test]
    public async Task Query_GroupBy_OrderByGroupField_ReturnsSortedResults()
    {
        var (_, key) = await SeedQueryContainer("q-grp-obf-db", "q-grp-obf-coll",
        [
            new { id = "f1", pk = "p1", category = "C" },
            new { id = "f2", pk = "p2", category = "A" },
            new { id = "f3", pk = "p3", category = "B" }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-obf-db", "q-grp-obf-coll",
            "SELECT c.category, COUNT(1) AS cnt FROM c GROUP BY c.category ORDER BY c.category ASC");
        var docs = result["Documents"]!.AsArray();

        Assert.That(docs.Count, Is.EqualTo(3));
        Assert.That(docs[0]!["category"]!.GetValue<string>(), Is.EqualTo("A"));
        Assert.That(docs[1]!["category"]!.GetValue<string>(), Is.EqualTo("B"));
        Assert.That(docs[2]!["category"]!.GetValue<string>(), Is.EqualTo("C"));
    }

    [Test]
    public async Task Query_GroupBy_OrderBySum_ReturnsSortedResults()
    {
        var (_, key) = await SeedQueryContainer("q-grp-obs-db", "q-grp-obs-coll",
        [
            new { id = "s1", pk = "p1", category = "A", amount = 5  },
            new { id = "s2", pk = "p2", category = "B", amount = 20 },
            new { id = "s3", pk = "p3", category = "A", amount = 10 }
        ]);

        var result = await ExecuteQueryAsync(key, "q-grp-obs-db", "q-grp-obs-coll",
            "SELECT c.category, SUM(c.amount) AS total FROM c GROUP BY c.category ORDER BY total DESC");
        var docs = result["Documents"]!.AsArray();

        Assert.That(docs.Count, Is.EqualTo(2));
        Assert.That(docs[0]!["category"]!.GetValue<string>(), Is.EqualTo("B"));
        Assert.That(docs[0]!["total"]!.GetValue<double>(), Is.EqualTo(20));
        Assert.That(docs[1]!["category"]!.GetValue<string>(), Is.EqualTo("A"));
        Assert.That(docs[1]!["total"]!.GetValue<double>(), Is.EqualTo(15));
    }

    [Test]
    public async Task ExpiredDocumentsPurgeScheduler_WhenDocumentTtlHasExpired_DocumentShouldBePurged()
    {
        // Arrange — create account, database, container via ARM so the scheduler can find them
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, AccountName, MinimalAccountContent());
        var account = accountResult.Value;

        var dbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlDatabaseResourceInfo("scheduler-ttl-db"));
        await account.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "scheduler-ttl-db", dbContent);

        var containerContent = new CosmosDBSqlContainerCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlContainerResourceInfo("scheduler-ttl-coll")
            {
                PartitionKey = new CosmosDBContainerPartitionKey { Paths = { "/pk" } },
                DefaultTtl = -1  // enable TTL on the container without a default
            });
        await account.GetCosmosDBSqlDatabases().Get("scheduler-ttl-db").Value
            .GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "scheduler-ttl-coll", containerContent);

        // Insert a document with ttl=1 via the Cosmos SDK data plane
        var keys = await account.GetKeysAsync();
        using var cosmosClient = CreateCosmosClient(account.Data.DocumentEndpoint!, keys.Value.PrimaryMasterKey!);
        _ownedClients.Add(cosmosClient);
        var container = cosmosClient.GetDatabase("scheduler-ttl-db").GetContainer("scheduler-ttl-coll");
        var doc = new { id = "expiring-doc", pk = "p1", ttl = 1 };
        await container.CreateItemAsync(doc, new PartitionKey("p1"));

        // Wait for the TTL to elapse
        await Task.Delay(TimeSpan.FromSeconds(2));

        var logger = new PrettyTopazLogger();
        var eventPipeline = new Pipeline(logger);
        var scheduler = new ExpiredDocumentsPurgeScheduler(
            eventPipeline,
            GlobalSettings.SoftDeletePurgeSchedulerInterval,
            logger);

        // Act
        await scheduler.ScanAndUpdateAsync();

        // Assert — reading the expired document should now throw NotFound
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await container.ReadItemAsync<dynamic>("expiring-doc", new PartitionKey("p1")));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DataPlane_WhenDisableLocalAuthIsTrue_Returns401()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-dp-disable-local-auth";

        var content = MinimalAccountContent();
        content.DisableLocalAuth = true;

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, content);

        var keys = await accountResult.Value.GetKeysAsync();
        var endpoint = accountResult.Value.Data.DocumentEndpoint!;
        var primaryKey = keys.Value.PrimaryMasterKey!;

        using var cosmosClient = CreateCosmosClient(endpoint, primaryKey);
        _ownedClients.Add(cosmosClient);

        // Act & Assert — master-key auth must be rejected when local auth is disabled
        var ex = Assert.ThrowsAsync<CosmosException>(async () =>
            await cosmosClient.CreateDatabaseAsync("some-db"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }
}
