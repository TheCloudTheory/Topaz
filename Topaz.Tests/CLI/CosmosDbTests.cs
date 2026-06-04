using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class CosmosDbTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("D1C2B3A4-0000-0000-0000-EE0700000009");
    private const string SubscriptionName = "sub-test-cosmosdb";
    private const string ResourceGroupName = "rg-test-cosmosdb";
    private const string AccountName = "test-cosmos-account";
    private const string DatabaseName = "test-database";

    private string AccountMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".azure-cosmos-db", AccountName, "metadata.json");

    private string DatabaseMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".azure-cosmos-db", AccountName, "sqldatabases", DatabaseName, "metadata.json");

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync([
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "cosmosdb", "account", "create",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "cosmosdb", "sql-database", "create",
            "--account-name", AccountName,
            "--database-name", DatabaseName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void CosmosDb_WhenAccountIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(AccountMetadataPath), Is.True);
    }

    [Test]
    public async Task CosmosDb_WhenAccountIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "get",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountIsUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "update",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--tags", "env=test"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountsAreListedByResourceGroup_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountsAreListedBySubscription_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "list-by-subscription",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountKeysAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "list-keys",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountReadOnlyKeysAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "list-readonly-keys",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountConnectionStringsAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "list-connection-strings",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenAccountIsDeleted_MetadataFileShouldNotExist()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "delete",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(File.Exists(AccountMetadataPath), Is.False);
        });
    }

    [Test]
    public void CosmosDb_WhenSqlDatabaseIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(DatabaseMetadataPath), Is.True);
    }

    [Test]
    public async Task CosmosDb_WhenSqlDatabaseIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "sql-database", "get",
            "--account-name", AccountName,
            "--database-name", DatabaseName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenSqlDatabasesAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "sql-database", "list",
            "--account-name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenSqlDatabaseThroughputIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "sql-database", "get-throughput",
            "--account-name", AccountName,
            "--database-name", DatabaseName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenSqlDatabaseThroughputIsUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "sql-database", "update-throughput",
            "--account-name", AccountName,
            "--database-name", DatabaseName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--throughput", "800"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task CosmosDb_WhenSqlDatabaseIsDeleted_MetadataFileShouldNotExist()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "sql-database", "delete",
            "--account-name", AccountName,
            "--database-name", DatabaseName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(File.Exists(DatabaseMetadataPath), Is.False);
        });
    }

    [Test]
    public async Task CosmosDb_WhenKeyIsRegenerated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "cosmosdb", "account", "regenerate-key",
            "--name", AccountName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--key-kind", "primary"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
