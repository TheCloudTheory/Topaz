namespace Topaz.Tests.AzureCLI;

public class NetworkInterfaceTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-nic";
    private const string VNetName = "vnet-cli-nic";
    private const string SubnetName = "default";
    private const string NicName = "my-cli-nic";

    [Test]
    public async Task NetworkInterfaceTests_WhenNICIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az network vnet create --name {VNetName} --resource-group {ResourceGroup} " +
            $"--address-prefix 10.30.0.0/16 --subnet-name {SubnetName} --subnet-prefix 10.30.0.0/24",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic create --name {NicName} --resource-group {ResourceGroup} " +
            $"--vnet-name {VNetName} --subnet {SubnetName}",
            response =>
            {
                var nic = response["NewNIC"] ?? response;
                Assert.Multiple(() =>
                {
                    Assert.That(nic["name"]!.GetValue<string>(), Is.EqualTo(NicName));
                    Assert.That(nic["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Network/networkInterfaces").IgnoreCase);
                    Assert.That(nic["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenNICIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az network vnet create --name {VNetName} --resource-group {ResourceGroup}-del " +
            $"--address-prefix 10.31.0.0/16 --subnet-name {SubnetName} --subnet-prefix 10.31.0.0/24",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic create --name {NicName}-del --resource-group {ResourceGroup}-del " +
            $"--vnet-name {VNetName} --subnet {SubnetName}",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic delete --name {NicName}-del --resource-group {ResourceGroup}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic show --name {NicName}-del --resource-group {ResourceGroup}-del",
            null, 3);
    }

    [Test]
    public async Task NetworkInterfaceTests_WhenNICsAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az network vnet create --name {VNetName} --resource-group {ResourceGroup}-list " +
            $"--address-prefix 10.32.0.0/16 --subnet-name {SubnetName} --subnet-prefix 10.32.0.0/24",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic create --name {NicName}-list-a --resource-group {ResourceGroup}-list " +
            $"--vnet-name {VNetName} --subnet {SubnetName}",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic create --name {NicName}-list-b --resource-group {ResourceGroup}-list " +
            $"--vnet-name {VNetName} --subnet {SubnetName}",
            null, 0);
        await RunAzureCliCommand(
            $"az network nic list --resource-group {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{NicName}-list-a"));
                    Assert.That(names, Does.Contain($"{NicName}-list-b"));
                });
            }, 0);
    }
}
