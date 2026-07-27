namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class LogAnalyticsTests : PowerShellTestBase
{
    [Test]
    public async Task LogAnalytics_WhenCreateCommandIsCalled_WorkspaceShouldBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-la-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzOperationalInsightsWorkspace -Name ps-la-create -ResourceGroupName ps-la-create-rg -Location westeurope -Sku PerGB2018 | ConvertTo-Json -Depth 5\n" +
            "Remove-AzOperationalInsightsWorkspace -Name ps-la-create -ResourceGroupName ps-la-create-rg -ForceDelete -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-la-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-la-create"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope").IgnoreCase);
                    Assert.That(response["ProvisioningState"]!.GetValue<string>(), Is.EqualTo("Succeeded").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task LogAnalytics_WhenGetCommandIsCalled_WorkspaceShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-la-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzOperationalInsightsWorkspace -Name ps-la-get -ResourceGroupName ps-la-get-rg -Location westeurope -Sku PerGB2018 | Out-Null\n" +
            "$result = Get-AzOperationalInsightsWorkspace -Name ps-la-get -ResourceGroupName ps-la-get-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzOperationalInsightsWorkspace -Name ps-la-get -ResourceGroupName ps-la-get-rg -ForceDelete -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-la-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-la-get"));
                    Assert.That(response["Sku"]!.GetValue<string>(), Is.EqualTo("pergb2018").IgnoreCase);
                    Assert.That(response["CustomerId"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                });
            });
    }

    [Test]
    public async Task LogAnalytics_WhenDeleteCommandIsCalled_WorkspaceShouldNotBeRetrievable()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-la-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzOperationalInsightsWorkspace -Name ps-la-del -ResourceGroupName ps-la-del-rg -Location westeurope -Sku PerGB2018 | Out-Null\n" +
            "Remove-AzOperationalInsightsWorkspace -Name ps-la-del -ResourceGroupName ps-la-del-rg -ForceDelete -Force | Out-Null\n" +
            "$result = (Get-AzOperationalInsightsWorkspace -Name ps-la-del -ResourceGroupName ps-la-del-rg -ErrorAction SilentlyContinue) -eq $null\n" +
            "Remove-AzResourceGroup -Name ps-la-del-rg -Force | Out-Null\n" +
            "$result | ConvertTo-Json",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task LogAnalytics_WhenListCommandIsCalled_AllWorkspacesShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-la-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzOperationalInsightsWorkspace -Name ps-la-list-a -ResourceGroupName ps-la-list-rg -Location westeurope -Sku PerGB2018 | Out-Null\n" +
            "New-AzOperationalInsightsWorkspace -Name ps-la-list-b -ResourceGroupName ps-la-list-rg -Location westeurope -Sku PerGB2018 | Out-Null\n" +
            "$result = Get-AzOperationalInsightsWorkspace -ResourceGroupName ps-la-list-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzOperationalInsightsWorkspace -Name ps-la-list-a -ResourceGroupName ps-la-list-rg -ForceDelete -Force | Out-Null\n" +
            "Remove-AzOperationalInsightsWorkspace -Name ps-la-list-b -ResourceGroupName ps-la-list-rg -ForceDelete -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-la-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var array = response.AsArray();
                var names = array!
                    .Select(n => n!["Name"]!.GetValue<string>())
                    .ToList();

                Assert.That(names, Does.Contain("ps-la-list-a"));
                Assert.That(names, Does.Contain("ps-la-list-b"));
            });
    }
}
