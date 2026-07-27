namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class CosmosDbTests : PowerShellTestBase
{
    [Test]
    public async Task CosmosDbTests_WhenNewAzCosmosDBAccountCommandIsCalled_AccountShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-cosmos-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzCosmosDBAccount -ResourceGroupName ps-cosmos-create-rg -Name 'ps-cosmos-account-01' " +
            "-Location westeurope -Confirm:$false | ConvertTo-Json -Depth 10\n" +
            "Remove-AzCosmosDBAccount -ResourceGroupName ps-cosmos-create-rg -Name 'ps-cosmos-account-01' -AsJob | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-cosmos-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-cosmos-account-01"));
                    Assert.That(response["Id"]!.GetValue<string>(),
                        Does.Contain("ps-cosmos-create-rg").IgnoreCase);
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope").IgnoreCase);
                    Assert.That(response["DocumentEndpoint"]!.GetValue<string>(),
                        Does.Contain(".documents.topaz.local.dev"));
                });
            });
    }

    [Test]
    public async Task CosmosDbTests_WhenGetAzCosmosDBAccountCommandIsCalled_AccountShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-cosmos-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzCosmosDBAccount -ResourceGroupName ps-cosmos-get-rg -Name 'ps-cosmos-account-02' " +
            "-Location westeurope -Confirm:$false | Out-Null\n" +
            "$result = Get-AzCosmosDBAccount -ResourceGroupName ps-cosmos-get-rg -Name 'ps-cosmos-account-02' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzCosmosDBAccount -ResourceGroupName ps-cosmos-get-rg -Name 'ps-cosmos-account-02' -AsJob | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-cosmos-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-cosmos-account-02"));
            });
    }

    [Test]
    public async Task CosmosDbTests_WhenRemoveAzCosmosDBAccountCommandIsCalled_AccountShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-cosmos-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzCosmosDBAccount -ResourceGroupName ps-cosmos-del-rg -Name 'ps-cosmos-account-03' " +
            "-Location westeurope -Confirm:$false | Out-Null\n" +
            "Remove-AzCosmosDBAccount -ResourceGroupName ps-cosmos-del-rg -Name 'ps-cosmos-account-03' -AsJob | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-cosmos-del-rg -Force | Out-Null\n" +
            "$true | ConvertTo-Json",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task CosmosDbTests_WhenNewAzCosmosDBSqlDatabaseCommandIsCalled_SqlDatabaseShouldBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-cosmos-sqldb-create-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzCosmosDBAccount -ResourceGroupName ps-cosmos-sqldb-create-rg -Name 'ps-cosmos-sqldb-01' " +
            "-Location westeurope -Confirm:$false | Out-Null\n" +
            "$result = New-AzCosmosDBSqlDatabase -ResourceGroupName ps-cosmos-sqldb-create-rg " +
            "-AccountName 'ps-cosmos-sqldb-01' -Name 'ps-sqldb-01' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResourceGroup -Name ps-cosmos-sqldb-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-sqldb-01"));
                    Assert.That(response["Id"]!.GetValue<string>(),
                        Does.Contain("ps-cosmos-sqldb-01").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task CosmosDbTests_WhenGetAzCosmosDBSqlDatabaseCommandIsCalled_SqlDatabaseShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-cosmos-sqldb-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzCosmosDBAccount -ResourceGroupName ps-cosmos-sqldb-get-rg -Name 'ps-cosmos-sqldb-02' " +
            "-Location westeurope -Confirm:$false | Out-Null\n" +
            "New-AzCosmosDBSqlDatabase -ResourceGroupName ps-cosmos-sqldb-get-rg " +
            "-AccountName 'ps-cosmos-sqldb-02' -Name 'ps-sqldb-02' | Out-Null\n" +
            "$result = Get-AzCosmosDBSqlDatabase -ResourceGroupName ps-cosmos-sqldb-get-rg " +
            "-AccountName 'ps-cosmos-sqldb-02' -Name 'ps-sqldb-02' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResourceGroup -Name ps-cosmos-sqldb-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-sqldb-02"));
            });
    }

    [Test]
    public async Task CosmosDbTests_WhenRemoveAzCosmosDBSqlDatabaseCommandIsCalled_SqlDatabaseShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-cosmos-sqldb-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzCosmosDBAccount -ResourceGroupName ps-cosmos-sqldb-del-rg -Name 'ps-cosmos-sqldb-03' " +
            "-Location westeurope -Confirm:$false | Out-Null\n" +
            "New-AzCosmosDBSqlDatabase -ResourceGroupName ps-cosmos-sqldb-del-rg " +
            "-AccountName 'ps-cosmos-sqldb-03' -Name 'ps-sqldb-03' | Out-Null\n" +
            "Remove-AzCosmosDBSqlDatabase -ResourceGroupName ps-cosmos-sqldb-del-rg " +
            "-AccountName 'ps-cosmos-sqldb-03' -Name 'ps-sqldb-03' -PassThru | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-cosmos-sqldb-del-rg -Force | Out-Null\n" +
            "$true | ConvertTo-Json",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }
}
