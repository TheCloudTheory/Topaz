namespace Topaz.Tests.AzureCLI;

public class SqlServerTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-sql";
    private const string ServerName = "my-cli-sql-server";

    [Test]
    public async Task SqlServerTests_WhenServerIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az sql server create --name {ServerName} --resource-group {ResourceGroup} " +
            $"--location westeurope --admin-user sqladmin --admin-password 'SqlAdmin1234!@#'",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(ServerName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Sql/servers").IgnoreCase);
                    Assert.That(response["state"]!.GetValue<string>(),
                        Is.EqualTo("Ready").IgnoreCase);
                    Assert.That(response["fullyQualifiedDomainName"]!.GetValue<string>(),
                        Does.Contain(".database.topaz.local.dev"));
                });
            }, 0);
    }

    [Test]
    public async Task SqlServerTests_WhenServerIsDeleted_ItShouldNotBeAvailable()
    {
        var rgDel = $"{ResourceGroup}-del";
        var serverDel = $"{ServerName}-del";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgDel}", null, 0);
        await RunAzureCliCommand(
            $"az sql server create --name {serverDel} --resource-group {rgDel} " +
            $"--location westeurope --admin-user sqladmin --admin-password 'SqlAdmin1234!@#'",
            null, 0);
        await RunAzureCliCommand(
            $"az sql server delete --name {serverDel} --resource-group {rgDel} --yes",
            null, 0);
        await RunAzureCliCommand(
            $"az sql server show --name {serverDel} --resource-group {rgDel}",
            null, 3);
    }

    [Test]
    public async Task SqlServerTests_WhenServersAreListed_AllShouldAppear()
    {
        var rgList = $"{ResourceGroup}-list";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgList}", null, 0);
        await RunAzureCliCommand(
            $"az sql server create --name {ServerName}-list-a --resource-group {rgList} " +
            $"--location westeurope --admin-user sqladmin --admin-password 'SqlAdmin1234!@#'",
            null, 0);
        await RunAzureCliCommand(
            $"az sql server create --name {ServerName}-list-b --resource-group {rgList} " +
            $"--location westeurope --admin-user sqladmin --admin-password 'SqlAdmin1234!@#'",
            null, 0);
        await RunAzureCliCommand(
            $"az sql server list --resource-group {rgList}",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{ServerName}-list-a"));
                    Assert.That(names, Does.Contain($"{ServerName}-list-b"));
                });
            }, 0);
    }
}
