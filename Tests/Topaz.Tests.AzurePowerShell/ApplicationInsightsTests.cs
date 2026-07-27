namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class ApplicationInsightsTests : PowerShellTestBase
{
    [Test]
    public async Task ApplicationInsights_WhenCreateCommandIsCalled_ComponentShouldBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-ai-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzApplicationInsights -Name ps-ai-create -ResourceGroupName ps-ai-create-rg -Location westeurope -Kind web | ConvertTo-Json -Depth 5\n" +
            "Remove-AzApplicationInsights -Name ps-ai-create -ResourceGroupName ps-ai-create-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-ai-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-ai-create"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope").IgnoreCase);
                    Assert.That(response["Kind"]!.GetValue<string>(), Is.EqualTo("web").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task ApplicationInsights_WhenGetCommandIsCalled_ComponentShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-ai-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzApplicationInsights -Name ps-ai-get -ResourceGroupName ps-ai-get-rg -Location westeurope -Kind web | Out-Null\n" +
            "$result = Get-AzApplicationInsights -Name ps-ai-get -ResourceGroupName ps-ai-get-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzApplicationInsights -Name ps-ai-get -ResourceGroupName ps-ai-get-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-ai-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-ai-get"));
                    Assert.That(response["Kind"]!.GetValue<string>(), Is.EqualTo("web").IgnoreCase);
                    Assert.That(response["InstrumentationKey"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                    Assert.That(response["ConnectionString"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                });
            });
    }

    [Test]
    public async Task ApplicationInsights_WhenDeleteCommandIsCalled_ComponentShouldNotBeRetrievable()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-ai-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzApplicationInsights -Name ps-ai-del -ResourceGroupName ps-ai-del-rg -Location westeurope -Kind web | Out-Null\n" +
            "Remove-AzApplicationInsights -Name ps-ai-del -ResourceGroupName ps-ai-del-rg | Out-Null\n" +
            "$result = (Get-AzApplicationInsights -Name ps-ai-del -ResourceGroupName ps-ai-del-rg -ErrorAction SilentlyContinue) -eq $null\n" +
            "Remove-AzResourceGroup -Name ps-ai-del-rg -Force | Out-Null\n" +
            "$result | ConvertTo-Json",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task ApplicationInsights_WhenListCommandIsCalled_AllComponentsShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-ai-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzApplicationInsights -Name ps-ai-list-a -ResourceGroupName ps-ai-list-rg -Location westeurope -Kind web | Out-Null\n" +
            "New-AzApplicationInsights -Name ps-ai-list-b -ResourceGroupName ps-ai-list-rg -Location westeurope -Kind web | Out-Null\n" +
            "$result = Get-AzApplicationInsights -ResourceGroupName ps-ai-list-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzApplicationInsights -Name ps-ai-list-a -ResourceGroupName ps-ai-list-rg | Out-Null\n" +
            "Remove-AzApplicationInsights -Name ps-ai-list-b -ResourceGroupName ps-ai-list-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-ai-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var array = response.AsArray();
                var names = array!
                    .Select(n => n!["Name"]!.GetValue<string>())
                    .ToList();

                Assert.That(names, Does.Contain("ps-ai-list-a"));
                Assert.That(names, Does.Contain("ps-ai-list-b"));
            });
    }
}
