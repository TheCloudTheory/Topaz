using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class SqlServerTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC001122");

    private const string SubscriptionName = "sub-test-sql";
    private const string ResourceGroupName = "rg-test-sql";
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
    }

    private static SqlServerData MinimalServerData() =>
        new(AzureLocation.WestEurope)
        {
            AdministratorLogin = AdminLogin,
            AdministratorLoginPassword = AdminPassword,
            Version = "12.0"
        };

    [Test]
    public async Task SqlServerTests_WhenCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string serverName = "test-sql-create";

        // Act
        var createResult = await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, serverName, MinimalServerData());

        var server = createResult.Value;

        // Assert
        Assert.That(server, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(server.Data.Name, Is.EqualTo(serverName));
            Assert.That(server.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Sql/servers")));
            Assert.That(server.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(server.Data.FullyQualifiedDomainName, Is.EqualTo($"{serverName}.database.topaz.local.dev"));
            Assert.That(server.Data.State, Is.EqualTo("Ready"));
        });
    }

    [Test]
    public async Task SqlServerTests_WhenDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string serverName = "test-sql-delete";

        await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, serverName, MinimalServerData());

        // Act
        var server = resourceGroup.Value.GetSqlServer(serverName);
        await server.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetSqlServerAsync(serverName));
    }

    [Test]
    public async Task SqlServerTests_WhenTagsAreUpdated_TagsShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string serverName = "test-sql-update";

        await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, serverName, MinimalServerData());

        var updatedData = MinimalServerData();
        updatedData.Tags.Add("env", "test");
        updatedData.Tags.Add("team", "platform");

        // Act
        var updateResult = await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, serverName, updatedData);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.Value.Data.Tags, Does.ContainKey("env").WithValue("test"));
            Assert.That(updateResult.Value.Data.Tags, Does.ContainKey("team").WithValue("platform"));
        });
    }

    [Test]
    public async Task SqlServerTests_WhenListedByResourceGroup_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-sql-list-a", MinimalServerData());
        await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-sql-list-b", MinimalServerData());

        // Act
        var servers = new List<SqlServerResource>();
        await foreach (var server in resourceGroup.Value.GetSqlServers().GetAllAsync())
            servers.Add(server);

        // Assert
        var names = servers.Select(s => s.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-sql-list-a"));
            Assert.That(names, Does.Contain("test-sql-list-b"));
        });
    }

    [Test]
    public async Task SqlServerTests_WhenListedBySubscription_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-sql-sub-a", MinimalServerData());
        await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-sql-sub-b", MinimalServerData());

        // Act
        var servers = new List<SqlServerResource>();
        await foreach (var server in subscription.GetSqlServersAsync())
            servers.Add(server);

        // Assert
        var names = servers.Select(s => s.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-sql-sub-a"));
            Assert.That(names, Does.Contain("test-sql-sub-b"));
        });
    }
}
