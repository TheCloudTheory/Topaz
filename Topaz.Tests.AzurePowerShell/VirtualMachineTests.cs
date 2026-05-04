namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class VirtualMachineTests : PowerShellTestBase
{
    [Test]
    public async Task VirtualMachineTests_WhenNewAzResourceCommandIsCalled_VMShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vm-create-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ hardwareProfile = @{ vmSize = 'Standard_D2s_v3' }; osProfile = @{ computerName = 'PsTestVm01'; adminUsername = 'adminuser' } }\n" +
            "$result = New-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-create-rg -Name PsTestVm01 -Location westeurope -Properties $props -ApiVersion '2024-07-01' -Force | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-create-rg -Name PsTestVm01 -ApiVersion '2024-07-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-vm-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsTestVm01"));
                    Assert.That(response["ResourceType"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Compute/virtualMachines").IgnoreCase);
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task VirtualMachineTests_WhenGetAzResourceCommandIsCalled_VMShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vm-get-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ hardwareProfile = @{ vmSize = 'Standard_D2s_v3' } }\n" +
            "New-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-get-rg -Name PsGetVm02 -Location westeurope -Properties $props -ApiVersion '2024-07-01' -Force | Out-Null\n" +
            "$result = Get-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-get-rg -Name PsGetVm02 -ApiVersion '2024-07-01' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-get-rg -Name PsGetVm02 -ApiVersion '2024-07-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-vm-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsGetVm02"));
            });
    }

    [Test]
    public async Task VirtualMachineTests_WhenRemoveAzResourceCommandIsCalled_VMShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vm-del-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ hardwareProfile = @{ vmSize = 'Standard_D2s_v3' } }\n" +
            "New-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-del-rg -Name PsDelVm03 -Location westeurope -Properties $props -ApiVersion '2024-07-01' -Force | Out-Null\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-del-rg -Name PsDelVm03 -ApiVersion '2024-07-01' -Force | Out-Null\n" +
            "$exists = (Get-AzResource -ResourceType 'Microsoft.Compute/virtualMachines' -ResourceGroupName ps-vm-del-rg -Name PsDelVm03 -ApiVersion '2024-07-01' -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzResourceGroup -Name ps-vm-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }
}
