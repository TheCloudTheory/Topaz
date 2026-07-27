namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class VirtualNetworkTests : PowerShellTestBase
{
    [Test]
    public async Task VirtualNetworkTests_WhenVnetWithSubnetIsCreated_SubnetIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vnet-subnet-rg -Location westeurope -Force | Out-Null\n" +
            "$subnetConfig = New-AzVirtualNetworkSubnetConfig -Name ps-subnet -AddressPrefix '10.60.1.0/24'\n" +
            "$result = New-AzVirtualNetwork -Name ps-vnet-with-subnet -ResourceGroupName ps-vnet-subnet-rg -Location westeurope -AddressPrefix '10.60.0.0/16' -Subnet $subnetConfig | ConvertTo-Json -Depth 10\n" +
            "$result",
            response =>
            {
                var subnets = response["Subnets"]!.AsArray();
                Assert.That(subnets, Is.Not.Null);
                Assert.That(subnets!.Count, Is.GreaterThanOrEqualTo(1));
                Assert.Multiple(() =>
                {
                    Assert.That(subnets[0]!["Name"]!.GetValue<string>(), Is.EqualTo("ps-subnet"));
                    Assert.That(subnets[0]!["AddressPrefix"]!.AsArray()[0]!.GetValue<string>(), Is.EqualTo("10.60.1.0/24"));
                });
            });
    }

    [Test]
    public async Task VirtualNetworkTests_WhenGetVnetIsCalled_VnetIsReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vnet-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzVirtualNetwork -Name ps-vnet-get -ResourceGroupName ps-vnet-get-rg -Location westeurope -AddressPrefix '10.61.0.0/16' | Out-Null\n" +
            "$result = Get-AzVirtualNetwork -Name ps-vnet-get -ResourceGroupName ps-vnet-get-rg | ConvertTo-Json -Depth 5\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-vnet-get"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(), Is.EqualTo("ps-vnet-get-rg"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                });
            });
    }

    [Test]
    public async Task VirtualNetworkTests_WhenIpIsInSubnet_IpIsAvailable()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vnet-checkip-rg -Location westeurope -Force | Out-Null\n" +
            "$subnetConfig = New-AzVirtualNetworkSubnetConfig -Name ps-checkip-subnet -AddressPrefix '10.63.1.0/24'\n" +
            "New-AzVirtualNetwork -Name ps-vnet-checkip -ResourceGroupName ps-vnet-checkip-rg -Location westeurope -AddressPrefix '10.63.0.0/16' -Subnet $subnetConfig | Out-Null\n" +
            "$result = Test-AzPrivateIPAddressAvailability -ResourceGroupName ps-vnet-checkip-rg -VirtualNetworkName ps-vnet-checkip -IPAddress '10.63.1.5' | ConvertTo-Json -Depth 5\n" +
            "$result",
            response =>
            {
                Assert.That(response["Available"]!.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task VirtualNetworkTests_WhenIpIsNotInAnySubnet_IpIsNotAvailable()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-vnet-checkip2-rg -Location westeurope -Force | Out-Null\n" +
            "$subnetConfig = New-AzVirtualNetworkSubnetConfig -Name ps-checkip2-subnet -AddressPrefix '10.64.1.0/24'\n" +
            "New-AzVirtualNetwork -Name ps-vnet-checkip2 -ResourceGroupName ps-vnet-checkip2-rg -Location westeurope -AddressPrefix '10.64.0.0/16' -Subnet $subnetConfig | Out-Null\n" +
            "$result = Test-AzPrivateIPAddressAvailability -ResourceGroupName ps-vnet-checkip2-rg -VirtualNetworkName ps-vnet-checkip2 -IPAddress '192.168.1.1' | ConvertTo-Json -Depth 5\n" +
            "$result",
            response =>
            {
                Assert.That(response["Available"]!.GetValue<bool>(), Is.False);
            });
    }
}
