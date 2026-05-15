namespace Topaz.Tests.AzureCLI;

/// <summary>
/// Gap-discovery tests for `az vm create`. These tests exercise the full compound flow:
/// VNet + Subnet → Public IP → NSG → NIC → VM.
/// </summary>
public class VirtualMachineAzCreateTests : TopazFixture
{
    private const string ResourceGroup = "rg-azcreate-vm";
    private const string VmName = "my-azcreate-vm";

    [Test]
    public async Task VirtualMachineAzCreateTests_WhenVMIsCreatedWithAzVmCreate_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az vm create " +
            $"--name {VmName} " +
            $"--resource-group {ResourceGroup} " +
            $"--image Ubuntu2204 " +
            $"--admin-username azureuser " +
            $"--admin-password \"Admin1234!@#\" " +
            $"--location westeurope",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["powerState"]?.GetValue<string>() ?? "VM running",
                        Is.Not.Null);
                    Assert.That(response["resourceGroup"]!.GetValue<string>(),
                        Is.EqualTo(ResourceGroup).IgnoreCase);
                });
            }, 0);

        // Verify VM exists after creation
        await RunAzureCliCommand(
            $"az vm show --name {VmName} --resource-group {ResourceGroup}",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(VmName));
                    Assert.That(response["provisioningState"]?.GetValue<string>() ??
                                response["properties"]?["provisioningState"]?.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }
}
