using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
namespace Topaz.Tests.E2E;

public class CosmosDbTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC002200");

    private const string SubscriptionName = "sub-test-cosmosdb";
    private const string ResourceGroupName = "rg-test-cosmosdb";

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

    private static CosmosDBAccountCreateOrUpdateContent MinimalAccountContent() =>
        new(AzureLocation.WestEurope, [new CosmosDBAccountLocation { LocationName = "westeurope" }]);

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    [Test]
    public async Task DatabaseAccount_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-create";

        // Act
        var createResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var account = createResult.Value;

        // Assert
        Assert.That(account, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(account.Data.Name, Is.EqualTo(accountName));
            Assert.That(account.Data.ResourceType.ToString(), Is.EqualTo("Microsoft.DocumentDB/databaseAccounts").IgnoreCase);
            Assert.That(account.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(account.Data.DocumentEndpoint, Does.Contain($"{accountName}.documents.topaz.local.dev"));
        });
    }

    [Test]
    public async Task DatabaseAccount_WhenRetrieved_IsFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-get";

        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        // Act
        var getResult = await resourceGroup.Value.GetCosmosDBAccountAsync(accountName);

        // Assert
        Assert.That(getResult.Value, Is.Not.Null);
        Assert.That(getResult.Value.Data.Name, Is.EqualTo(accountName));
    }

    [Test]
    public async Task DatabaseAccount_WhenUpdated_HasNewTags()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-update";

        var createContent = MinimalAccountContent();
        createContent.Tags.Add("env", "test");
        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, createContent);

        // Act
        var updateContent = MinimalAccountContent();
        updateContent.Tags.Add("env", "updated");
        var updateResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, updateContent);

        // Assert
        Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("updated"));
    }

    [Test]
    public async Task DatabaseAccount_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-delete";

        var createResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        // Act
        await createResult.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetCosmosDBAccountAsync(accountName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task DatabaseAccount_WhenListedByResourceGroup_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-cosmos-list-a", MinimalAccountContent());
        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-cosmos-list-b", MinimalAccountContent());

        // Act
        var accounts = resourceGroup.Value.GetCosmosDBAccounts().GetAllAsync();
        var names = new List<string>();
        await foreach (var account in accounts)
        {
            names.Add(account.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("test-cosmos-list-a"));
        Assert.That(names, Does.Contain("test-cosmos-list-b"));
    }

    [Test]
    public async Task DatabaseAccount_WhenListedBySubscription_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-cosmos-sub-list-a", MinimalAccountContent());

        // Act
        var accounts = subscription.GetCosmosDBAccountsAsync();
        var names = new List<string>();
        await foreach (var account in accounts)
        {
            names.Add(account.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("test-cosmos-sub-list-a"));
    }

    [Test]
    public async Task SqlDatabase_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-create";
        const string databaseName = "mydb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var createContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlDatabaseResourceInfo(databaseName))
        {
            Options = new CosmosDBCreateUpdateConfig { Throughput = 400 }
        };

        // Act
        var dbResult = await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, createContent);

        // Assert
        Assert.That(dbResult.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(dbResult.Value.Data.Name, Is.EqualTo(databaseName));
            Assert.That(dbResult.Value.Data.ResourceType.ToString(),
                Is.EqualTo("Microsoft.DocumentDB/databaseAccounts/sqlDatabases").IgnoreCase);
            Assert.That(dbResult.Value.Data.Resource.DatabaseName, Is.EqualTo(databaseName));
        });
    }

    [Test]
    public async Task SqlDatabase_WhenRetrieved_IsFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-get";
        const string databaseName = "getdb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        // Act
        var getResult = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);

        // Assert
        Assert.That(getResult.Value.Data.Name, Is.EqualTo(databaseName));
    }

    [Test]
    public async Task SqlDatabase_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-delete";
        const string databaseName = "deletedb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var dbResult = await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        // Act
        await dbResult.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task SqlDatabase_WhenListed_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-list";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "listdb-a",
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo("listdb-a")));
        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "listdb-b",
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo("listdb-b")));

        // Act
        var databases = accountResult.Value.GetCosmosDBSqlDatabases().GetAllAsync();
        var names = new List<string>();
        await foreach (var db in databases)
        {
            names.Add(db.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("listdb-a"));
        Assert.That(names, Does.Contain("listdb-b"));
    }

    [Test]
    public async Task SqlDatabase_WhenThroughputUpdated_ReflectsNewValue()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-throughput";
        const string databaseName = "throughputdb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var dbResult = await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName))
                {
                    Options = new CosmosDBCreateUpdateConfig { Throughput = 400 }
                });

        // Act
        var updateContent = new ThroughputSettingsUpdateData(
            AzureLocation.WestEurope,
            new ThroughputSettingsResourceInfo { Throughput = 800 });
        var throughputResult = await dbResult.Value.GetCosmosDBSqlDatabaseThroughputSetting()
            .CreateOrUpdateAsync(WaitUntil.Completed, updateContent);

        // Assert
        Assert.That(throughputResult.Value.Data.Resource.Throughput, Is.EqualTo(800));
    }

    [Test]
    public async Task SqlDatabase_WhenThroughputFetched_ReturnsThroughputSettings()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-getthroughput";
        const string databaseName = "getthroughputdb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName))
                {
                    Options = new CosmosDBCreateUpdateConfig { Throughput = 600 }
                });

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);

        // Act
        var throughputResult = await database.Value.GetCosmosDBSqlDatabaseThroughputSetting().GetAsync();

        // Assert
        Assert.That(throughputResult.Value.Data.Resource.Throughput, Is.EqualTo(600));
    }
}
