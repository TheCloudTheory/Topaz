namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class PublicIpAddressTests : PowerShellTestBase
{
    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPIsCreated_PublicIPIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-pip-create-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ publicIPAllocationMethod = 'Dynamic'; publicIPAddressVersion = 'IPv4' }\n" +
            "$result = New-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-create-rg -Name ps-pip -Location westeurope -Properties $props -ApiVersion '2023-09-01' -Force | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-create-rg -Name ps-pip -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-pip-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-pip"));
                    Assert.That(response["ResourceType"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Network/publicIPAddresses").IgnoreCase);
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task PublicIpAddressTests_WhenGetPublicIPIsCalled_PublicIPIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-pip-get-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ publicIPAllocationMethod = 'Dynamic' }\n" +
            "New-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-get-rg -Name ps-pip-get -Location westeurope -Properties $props -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "$result = Get-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-get-rg -Name ps-pip-get -ApiVersion '2023-09-01' | ConvertTo-Json -Depth 10\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-get-rg -Name ps-pip-get -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-pip-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-pip-get"));
            });
    }

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPIsDeleted_PublicIPIsNotFound()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-pip-del-rg -Location westeurope -Force | Out-Null\n" +
            "$props = @{ publicIPAllocationMethod = 'Dynamic' }\n" +
            "New-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-del-rg -Name ps-pip-del -Location westeurope -Properties $props -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "Remove-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-del-rg -Name ps-pip-del -ApiVersion '2023-09-01' -Force | Out-Null\n" +
            "$exists = (Get-AzResource -ResourceType 'Microsoft.Network/publicIPAddresses' -ResourceGroupName ps-pip-del-rg -Name ps-pip-del -ApiVersion '2023-09-01' -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzResourceGroup -Name ps-pip-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }
}
