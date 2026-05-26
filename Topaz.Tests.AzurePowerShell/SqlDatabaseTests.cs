namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class SqlDatabaseTests : PowerShellTestBase
{
    [Test]
    public async Task SqlDatabaseTests_WhenNewAzSqlDatabaseCommandIsCalled_DatabaseShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-sql-db-create-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzSqlServer -ResourceGroupName ps-sql-db-create-rg -ServerName 'ps-sql-db-server-01' " +
            "-Location westeurope -SqlAdministratorCredentials (New-Object -TypeName System.Management.Automation.PSCredential " +
            "-ArgumentList 'sqladmin', (ConvertTo-SecureString 'SqlAdmin1234!@#' -AsPlainText -Force)) | Out-Null\n" +
            "$result = New-AzSqlDatabase -ResourceGroupName ps-sql-db-create-rg -ServerName 'ps-sql-db-server-01' " +
            "-DatabaseName 'ps-sql-database-01' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzSqlServer -ResourceGroupName ps-sql-db-create-rg -ServerName 'ps-sql-db-server-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-sql-db-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["DatabaseName"]!.GetValue<string>(), Is.EqualTo("ps-sql-database-01"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(),
                        Is.EqualTo("ps-sql-db-create-rg").IgnoreCase);
                    Assert.That(response["ServerName"]!.GetValue<string>(),
                        Is.EqualTo("ps-sql-db-server-01").IgnoreCase);
                    Assert.That(response["Status"]!.GetValue<string>(), Is.EqualTo("Online").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task SqlDatabaseTests_WhenGetAzSqlDatabaseCommandIsCalled_DatabaseShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-sql-db-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzSqlServer -ResourceGroupName ps-sql-db-get-rg -ServerName 'ps-sql-db-server-02' " +
            "-Location westeurope -SqlAdministratorCredentials (New-Object -TypeName System.Management.Automation.PSCredential " +
            "-ArgumentList 'sqladmin', (ConvertTo-SecureString 'SqlAdmin1234!@#' -AsPlainText -Force)) | Out-Null\n" +
            "New-AzSqlDatabase -ResourceGroupName ps-sql-db-get-rg -ServerName 'ps-sql-db-server-02' " +
            "-DatabaseName 'ps-sql-database-02' | Out-Null\n" +
            "$result = Get-AzSqlDatabase -ResourceGroupName ps-sql-db-get-rg -ServerName 'ps-sql-db-server-02' " +
            "-DatabaseName 'ps-sql-database-02' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzSqlServer -ResourceGroupName ps-sql-db-get-rg -ServerName 'ps-sql-db-server-02' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-sql-db-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["DatabaseName"]!.GetValue<string>(), Is.EqualTo("ps-sql-database-02"));
            });
    }

    [Test]
    public async Task SqlDatabaseTests_WhenRemoveAzSqlDatabaseCommandIsCalled_DatabaseShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-sql-db-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzSqlServer -ResourceGroupName ps-sql-db-del-rg -ServerName 'ps-sql-db-server-03' " +
            "-Location westeurope -SqlAdministratorCredentials (New-Object -TypeName System.Management.Automation.PSCredential " +
            "-ArgumentList 'sqladmin', (ConvertTo-SecureString 'SqlAdmin1234!@#' -AsPlainText -Force)) | Out-Null\n" +
            "New-AzSqlDatabase -ResourceGroupName ps-sql-db-del-rg -ServerName 'ps-sql-db-server-03' " +
            "-DatabaseName 'ps-sql-database-03' | Out-Null\n" +
            "Remove-AzSqlDatabase -ResourceGroupName ps-sql-db-del-rg -ServerName 'ps-sql-db-server-03' " +
            "-DatabaseName 'ps-sql-database-03' -Force | Out-Null\n" +
            "$exists = (Get-AzSqlDatabase -ResourceGroupName ps-sql-db-del-rg -ServerName 'ps-sql-db-server-03' " +
            "-DatabaseName 'ps-sql-database-03' -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzSqlServer -ResourceGroupName ps-sql-db-del-rg -ServerName 'ps-sql-db-server-03' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-sql-db-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }
}
