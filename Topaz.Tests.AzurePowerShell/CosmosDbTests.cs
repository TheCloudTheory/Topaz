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
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(),
                        Is.EqualTo("ps-cosmos-create-rg").IgnoreCase);
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
            "$true",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }
}
