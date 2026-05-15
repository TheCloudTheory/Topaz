namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class NetworkInterfaceTests : PowerShellTestBase
{
    [Test]
    public async Task NetworkInterfaceTests_WhenNICIsCreated_NICIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-nic-create-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ ipConfigurations = @(@{ name = 'ipconfig1'; properties = @{ privateIPAllocationMethod = 'Dynamic' } }) }\n" +
            "$result = New-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-create-rg -Name ps-nic -Location westeurope -Properties $props -ApiVersion '2023-09-01' -Force | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-create-rg -Name ps-nic -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-nic-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-nic"));
                    Assert.That(response["ResourceType"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Network/networkInterfaces").IgnoreCase);
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenGetNICIsCalled_NICIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-nic-get-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ ipConfigurations = @(@{ name = 'ipconfig1'; properties = @{ privateIPAllocationMethod = 'Dynamic' } }) }\n" +
            "New-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-get-rg -Name ps-nic-get -Location westeurope -Properties $props -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "$result = Get-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-get-rg -Name ps-nic-get -ApiVersion '2023-09-01' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-get-rg -Name ps-nic-get -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-nic-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-nic-get"));
            });
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenNICIsDeleted_NICIsNotFound()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-nic-del-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ ipConfigurations = @(@{ name = 'ipconfig1'; properties = @{ privateIPAllocationMethod = 'Dynamic' } }) }\n" +
            "New-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-del-rg -Name ps-nic-del -Location westeurope -Properties $props -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-del-rg -Name ps-nic-del -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "$exists = (Get-AzResource -ResourceType 'Microsoft.Network/networkInterfaces' -ResourceGroupName ps-nic-del-rg -Name ps-nic-del -ApiVersion '2023-09-01' -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzResourceGroup -Name ps-nic-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }
}
