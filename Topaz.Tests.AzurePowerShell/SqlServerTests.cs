namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class SqlServerTests : PowerShellTestBase
{
    [Test]
    public async Task SqlServerTests_WhenNewAzSqlServerCommandIsCalled_ServerShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-sql-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzSqlServer -ResourceGroupName ps-sql-create-rg -ServerName 'ps-sql-server-01' " +
            "-Location westeurope -SqlAdministratorCredentials (New-Object -TypeName System.Management.Automation.PSCredential " +
            "-ArgumentList 'sqladmin', (ConvertTo-SecureString 'SqlAdmin1234!@#' -AsPlainText -Force)) | ConvertTo-Json -Depth 10\n" +
            "Remove-AzSqlServer -ResourceGroupName ps-sql-create-rg -ServerName 'ps-sql-server-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-sql-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["ServerName"]!.GetValue<string>(), Is.EqualTo("ps-sql-server-01"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(),
                        Is.EqualTo("ps-sql-create-rg").IgnoreCase);
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task SqlServerTests_WhenGetAzSqlServerCommandIsCalled_ServerShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-sql-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzSqlServer -ResourceGroupName ps-sql-get-rg -ServerName 'ps-sql-server-02' " +
            "-Location westeurope -SqlAdministratorCredentials (New-Object -TypeName System.Management.Automation.PSCredential " +
            "-ArgumentList 'sqladmin', (ConvertTo-SecureString 'SqlAdmin1234!@#' -AsPlainText -Force)) | Out-Null\n" +
            "$result = Get-AzSqlServer -ResourceGroupName ps-sql-get-rg -ServerName 'ps-sql-server-02' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzSqlServer -ResourceGroupName ps-sql-get-rg -ServerName 'ps-sql-server-02' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-sql-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["ServerName"]!.GetValue<string>(), Is.EqualTo("ps-sql-server-02"));
            });
    }

    [Test]
    public async Task SqlServerTests_WhenRemoveAzSqlServerCommandIsCalled_ServerShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-sql-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzSqlServer -ResourceGroupName ps-sql-del-rg -ServerName 'ps-sql-server-03' " +
            "-Location westeurope -SqlAdministratorCredentials (New-Object -TypeName System.Management.Automation.PSCredential " +
            "-ArgumentList 'sqladmin', (ConvertTo-SecureString 'SqlAdmin1234!@#' -AsPlainText -Force)) | Out-Null\n" +
            "Remove-AzSqlServer -ResourceGroupName ps-sql-del-rg -ServerName 'ps-sql-server-03' -Force | Out-Null\n" +
            "$exists = (Get-AzSqlServer -ResourceGroupName ps-sql-del-rg -ServerName 'ps-sql-server-03' -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzResourceGroup -Name ps-sql-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }
}
