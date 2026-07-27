namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class NetworkSecurityGroupTests : PowerShellTestBase
{
    [Test]
    public async Task NetworkSecurityGroupTests_WhenNsgIsCreated_NsgIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-nsg-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzNetworkSecurityGroup -Name ps-nsg -ResourceGroupName ps-nsg-create-rg -Location westeurope | ConvertTo-Json -Depth 5\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-nsg"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(), Is.EqualTo("ps-nsg-create-rg"));
                });
            });
    }

    [Test]
    public async Task NetworkSecurityGroupTests_WhenGetNsgIsCalled_NsgIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-nsg-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzNetworkSecurityGroup -Name ps-nsg-get -ResourceGroupName ps-nsg-get-rg -Location westeurope | Out-Null\n" +
            "$result = Get-AzNetworkSecurityGroup -Name ps-nsg-get -ResourceGroupName ps-nsg-get-rg | ConvertTo-Json -Depth 5\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-nsg-get"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(), Is.EqualTo("ps-nsg-get-rg"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                });
            });
    }

    [Test]
    public async Task NetworkSecurityGroupTests_WhenNsgHasDefaultRules_DefaultRulesAreReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-nsg-rules-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzNetworkSecurityGroup -Name ps-nsg-rules -ResourceGroupName ps-nsg-rules-rg -Location westeurope | ConvertTo-Json -Depth 5\n" +
            "$result",
            response =>
            {
                var defaultRules = response["DefaultSecurityRules"]!.AsArray();
                Assert.That(defaultRules, Is.Not.Null);
                Assert.That(defaultRules!.Count, Is.EqualTo(6));
            });
    }
}
