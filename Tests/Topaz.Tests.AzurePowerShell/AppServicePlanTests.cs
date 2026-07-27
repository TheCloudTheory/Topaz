namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class AppServicePlanTests : PowerShellTestBase
{
    [Test]
    public async Task AppServicePlan_WhenCreateCommandIsCalled_PlanShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-asp-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzAppServicePlan -Name ps-asp-create -ResourceGroupName ps-asp-create-rg -Location westeurope -Tier Basic -NumberofWorkers 1 -WorkerSize Small | ConvertTo-Json -Depth 5\n" +
            "Remove-AzAppServicePlan -Name ps-asp-create -ResourceGroupName ps-asp-create-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-asp-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                // New-AzAppServicePlan (old MAML SDK) does not populate Sku/ResourceGroup
                // from the ARM response body — those are verified in the Get test.
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-asp-create"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task AppServicePlan_WhenGetCommandIsCalled_PlanShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-asp-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzAppServicePlan -Name ps-asp-get -ResourceGroupName ps-asp-get-rg -Location westeurope -Tier Standard -NumberofWorkers 1 -WorkerSize Small | Out-Null\n" +
            "$result = Get-AzAppServicePlan -Name ps-asp-get -ResourceGroupName ps-asp-get-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzAppServicePlan -Name ps-asp-get -ResourceGroupName ps-asp-get-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-asp-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-asp-get"));
                    Assert.That(response["Sku"]!["Name"]!.GetValue<string>(), Is.EqualTo("S1").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task AppServicePlan_WhenListCommandIsCalled_AllPlansShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-asp-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzAppServicePlan -Name ps-asp-list-a -ResourceGroupName ps-asp-list-rg -Location westeurope -Tier Basic -NumberofWorkers 1 -WorkerSize Small | Out-Null\n" +
            "New-AzAppServicePlan -Name ps-asp-list-b -ResourceGroupName ps-asp-list-rg -Location westeurope -Tier Basic -NumberofWorkers 1 -WorkerSize Small | Out-Null\n" +
            "$result = Get-AzAppServicePlan -ResourceGroupName ps-asp-list-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzAppServicePlan -Name ps-asp-list-a -ResourceGroupName ps-asp-list-rg -Force | Out-Null\n" +
            "Remove-AzAppServicePlan -Name ps-asp-list-b -ResourceGroupName ps-asp-list-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-asp-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var array = response.AsArray();
                var names = array!
                    .Select(n => n!["Name"]!.GetValue<string>())
                    .ToList();

                Assert.That(names, Does.Contain("ps-asp-list-a"));
                Assert.That(names, Does.Contain("ps-asp-list-b"));
            });
    }
}
