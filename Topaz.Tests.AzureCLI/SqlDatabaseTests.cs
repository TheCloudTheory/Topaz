namespace Topaz.Tests.AzureCLI;

public class SqlDatabaseTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-sql-db";
    private const string ServerName = "my-cli-sql-db-server";
    private const string DatabaseName = "my-cli-database";

    [OneTimeSetUp]
    public async Task CreateServer()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az sql server create --name {ServerName} --resource-group {ResourceGroup} " +
            $"--location westeurope --admin-user sqladmin --admin-password 'SqlAdmin1234!@#'",
            null, 0);
    }

    [Test]
    public async Task SqlDatabaseTests_WhenDatabaseIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand(
            $"az sql db create --name {DatabaseName} --server {ServerName} --resource-group {ResourceGroup}",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(DatabaseName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Sql/servers/databases").IgnoreCase);
                    Assert.That(response["properties"]!["status"]!.GetValue<string>(),
                        Is.EqualTo("Online").IgnoreCase);
                });
            }, 0);
    }

    [Test]
    public async Task SqlDatabaseTests_WhenDatabaseIsDeleted_ItShouldNotBeAvailable()
    {
        var dbDel = $"{DatabaseName}-del";
        await RunAzureCliCommand(
            $"az sql db create --name {dbDel} --server {ServerName} --resource-group {ResourceGroup}",
            null, 0);
        await RunAzureCliCommand(
            $"az sql db delete --name {dbDel} --server {ServerName} --resource-group {ResourceGroup} --yes",
            null, 0);
        await RunAzureCliCommand(
            $"az sql db show --name {dbDel} --server {ServerName} --resource-group {ResourceGroup}",
            null, 3);
    }

    [Test]
    public async Task SqlDatabaseTests_WhenDatabasesAreListed_AllShouldAppear()
    {
        var dbListA = $"{DatabaseName}-list-a";
        var dbListB = $"{DatabaseName}-list-b";
        await RunAzureCliCommand(
            $"az sql db create --name {dbListA} --server {ServerName} --resource-group {ResourceGroup}",
            null, 0);
        await RunAzureCliCommand(
            $"az sql db create --name {dbListB} --server {ServerName} --resource-group {ResourceGroup}",
            null, 0);
        await RunAzureCliCommand(
            $"az sql db list --server {ServerName} --resource-group {ResourceGroup}",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain(dbListA));
                    Assert.That(names, Does.Contain(dbListB));
                });
            }, 0);
    }

    [Test]
    public async Task SqlDatabaseTests_WhenDatabaseIsShown_PropertiesShouldBeCorrect()
    {
        var dbShow = $"{DatabaseName}-show";
        await RunAzureCliCommand(
            $"az sql db create --name {dbShow} --server {ServerName} --resource-group {ResourceGroup}",
            null, 0);
        await RunAzureCliCommand(
            $"az sql db show --name {dbShow} --server {ServerName} --resource-group {ResourceGroup}",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(dbShow));
                    Assert.That(response["properties"]!["status"]!.GetValue<string>(),
                        Is.EqualTo("Online").IgnoreCase);
                    Assert.That(response["properties"]!["collation"]!.GetValue<string>(),
                        Is.EqualTo("SQL_Latin1_General_CP1_CI_AS").IgnoreCase);
                });
            }, 0);
    }
}
