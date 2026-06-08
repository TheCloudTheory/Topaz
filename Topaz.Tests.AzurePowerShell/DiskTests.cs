namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class DiskTests : PowerShellTestBase
{
    [Test]
    public async Task DiskTests_WhenNewAzResourceCommandIsCalled_DiskShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-disk-create-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ diskSizeGB = 32; creationData = @{ createOption = 'Empty' } }\n" +
            "$result = New-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-create-rg -Name PsTestDisk01 -Location westeurope -Properties $props -ApiVersion '2025-11-01' -Force | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-create-rg -Name PsTestDisk01 -ApiVersion '2025-11-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-disk-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsTestDisk01"));
                    Assert.That(response["ResourceType"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Compute/disks").IgnoreCase);
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task DiskTests_WhenGetAzResourceCommandIsCalled_DiskShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-disk-get-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ diskSizeGB = 32; creationData = @{ createOption = 'Empty' } }\n" +
            "New-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-get-rg -Name PsGetDisk02 -Location westeurope -Properties $props -ApiVersion '2025-11-01' -Force | Out-Null\n" +
            "$result = Get-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-get-rg -Name PsGetDisk02 -ApiVersion '2025-11-01' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-get-rg -Name PsGetDisk02 -ApiVersion '2025-11-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-disk-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsGetDisk02"));
            });
    }

    [Test]
    public async Task DiskTests_WhenRemoveAzResourceCommandIsCalled_DiskShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-disk-del-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ diskSizeGB = 32; creationData = @{ createOption = 'Empty' } }\n" +
            "New-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-del-rg -Name PsDelDisk03 -Location westeurope -Properties $props -ApiVersion '2025-11-01' -Force | Out-Null\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-del-rg -Name PsDelDisk03 -ApiVersion '2025-11-01' -Force | Out-Null\n" +
            "$exists = (Get-AzResource -ResourceType 'Microsoft.Compute/disks' -ResourceGroupName ps-disk-del-rg -Name PsDelDisk03 -ApiVersion '2025-11-01' -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzResourceGroup -Name ps-disk-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }
}
