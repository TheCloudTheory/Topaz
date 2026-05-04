namespace Topaz.Tests.AzureCLI;

public class VirtualMachineTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-vm";
    private const string VmName = "my-cli-vm";
    private const string VmBody =
        """{"location":"westeurope","properties":{"hardwareProfile":{"vmSize":"Standard_D2s_v3"},"osProfile":{"computerName":"my-cli-vm","adminUsername":"adminuser"}}}""";

    [Test]
    public async Task VirtualMachineTests_WhenVMIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/virtualMachines --api-version 2024-07-01 " +
            $"--resource-group {ResourceGroup} --name {VmName} --location westeurope " +
            $"--properties '{{\"hardwareProfile\":{{\"vmSize\":\"Standard_D2s_v3\"}},\"osProfile\":{{\"computerName\":\"{VmName}\",\"adminUsername\":\"adminuser\"}}}}'",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(VmName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Compute/virtualMachines").IgnoreCase);
                    Assert.That(response["properties"]!["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/virtualMachines --api-version 2024-07-01 " +
            $"--resource-group {ResourceGroup}-del --name {VmName}-del --location westeurope " +
            $"--properties '{{\"hardwareProfile\":{{\"vmSize\":\"Standard_D2s_v3\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource delete --resource-type Microsoft.Compute/virtualMachines --api-version 2024-07-01 " +
            $"--resource-group {ResourceGroup}-del --name {VmName}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az resource show --resource-type Microsoft.Compute/virtualMachines --api-version 2024-07-01 " +
            $"--resource-group {ResourceGroup}-del --name {VmName}-del",
            null, 3);
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMsAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/virtualMachines --api-version 2024-07-01 " +
            $"--resource-group {ResourceGroup}-list --name {VmName}-list-a --location westeurope " +
            $"--properties '{{\"hardwareProfile\":{{\"vmSize\":\"Standard_D2s_v3\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/virtualMachines --api-version 2024-07-01 " +
            $"--resource-group {ResourceGroup}-list --name {VmName}-list-b --location westeurope " +
            $"--properties '{{\"hardwareProfile\":{{\"vmSize\":\"Standard_D2s_v3\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource list --resource-group {ResourceGroup}-list " +
            $"--resource-type Microsoft.Compute/virtualMachines",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{VmName}-list-a"));
                    Assert.That(names, Does.Contain($"{VmName}-list-b"));
                });
            }, 0);
    }
}
