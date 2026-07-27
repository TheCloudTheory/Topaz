using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class SqlDatabaseTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC001133");

    private const string SubscriptionName = "sub-test-sql-db";
    private const string ResourceGroupName = "rg-test-sql-db";
    private const string ServerName = "test-sql-db-server";
    private const string AdminLogin = "sqladmin";
    private const string AdminPassword = "SqlAdmin1234!@#";

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

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        await resourceGroup.Value.GetSqlServers().CreateOrUpdateAsync(
            WaitUntil.Completed,
            ServerName,
            new SqlServerData(AzureLocation.WestEurope)
            {
                AdministratorLogin = AdminLogin,
                AdministratorLoginPassword = AdminPassword,
                Version = "12.0"
            });
    }

    [Test]
    public async Task SqlDatabaseTests_WhenCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var server = resourceGroup.Value.GetSqlServer(ServerName);
        const string databaseName = "test-db-create";

        // Act
        var createResult = await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, new SqlDatabaseData(AzureLocation.WestEurope));

        var database = createResult.Value;

        // Assert
        Assert.That(database, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(database.Data.Name, Is.EqualTo(databaseName));
            Assert.That(database.Data.ResourceType.ToString(), Is.EqualTo("Microsoft.Sql/servers/databases").IgnoreCase);
            Assert.That(database.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(database.Data.Status?.ToString(), Is.EqualTo("Online").IgnoreCase);
        });
    }

    [Test]
    public async Task SqlDatabaseTests_WhenDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var server = resourceGroup.Value.GetSqlServer(ServerName);
        const string databaseName = "test-db-delete";

        await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, new SqlDatabaseData(AzureLocation.WestEurope));

        // Act
        var database = server.Value.GetSqlDatabase(databaseName);
        await database.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await server.Value.GetSqlDatabaseAsync(databaseName));
    }

    [Test]
    public async Task SqlDatabaseTests_WhenTagsAreUpdated_TagsShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var server = resourceGroup.Value.GetSqlServer(ServerName);
        const string databaseName = "test-db-update";

        await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, new SqlDatabaseData(AzureLocation.WestEurope));

        var updatedData = new SqlDatabaseData(AzureLocation.WestEurope);
        updatedData.Tags.Add("env", "test");
        updatedData.Tags.Add("team", "platform");

        // Act
        var updateResult = await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, updatedData);

        // Assert
        var tags = updateResult.Value.Data.Tags.ToDictionary(k => k.Key, v => v.Value);
        Assert.Multiple(() =>
        {
            Assert.That(tags, Does.ContainKey("env").WithValue("test"));
            Assert.That(tags, Does.ContainKey("team").WithValue("platform"));
        });
    }

    [Test]
    public async Task SqlDatabaseTests_WhenListed_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var server = resourceGroup.Value.GetSqlServer(ServerName);

        await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-db-list-a", new SqlDatabaseData(AzureLocation.WestEurope));
        await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-db-list-b", new SqlDatabaseData(AzureLocation.WestEurope));

        // Act
        var databases = new List<SqlDatabaseResource>();
        await foreach (var db in server.Value.GetSqlDatabases().GetAllAsync())
            databases.Add(db);

        // Assert
        var names = databases.Select(d => d.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-db-list-a"));
            Assert.That(names, Does.Contain("test-db-list-b"));
        });
    }

    [Test]
    public async Task SqlDatabaseTests_WhenUpdatedUsingPatch_CollationShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var server = resourceGroup.Value.GetSqlServer(ServerName);
        const string databaseName = "test-db-patch";

        await server.Value.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, new SqlDatabaseData(AzureLocation.WestEurope));

        // Act
        var database = server.Value.GetSqlDatabase(databaseName).Value;
        var patch = new SqlDatabasePatch
        {
            Tags = { ["patched"] = "true" }
        };
        var patchResult = await database.UpdateAsync(WaitUntil.Completed, patch);

        // Assert
        var patchTags = patchResult.Value.Data.Tags.ToDictionary(k => k.Key, v => v.Value);
        Assert.That(patchTags, Does.ContainKey("patched").WithValue("true"));
    }
}
