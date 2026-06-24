using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Topaz.Identity;
using Xunit;

namespace Topaz.Example.IaCTesting;

[Collection("Topaz")]
public class BicepIaCTests
{
    private readonly ArmClient _arm;

    public BicepIaCTests()
    {
        _arm = new ArmClient(
            new AzureLocalCredential(Globals.GlobalAdminId),
            "00000000-0000-0000-0000-000000000001",
            Topaz.ResourceManager.TopazArmClientOptions.New);
    }

    [Fact]
    public async Task Deploy_StorageBicep_ShouldProvisionWithCorrectSku()
    {
        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;

        RunAzCli("deployment group create " +
                 "--resource-group rg-iac-test " +
                 "--template-file bicep/storage.bicep");

        var storage = (await rg.GetStorageAccountAsync("stbiceptest")).Value;

        Assert.Equal("Standard_LRS", storage.Data.Sku.Name.ToString());
        Assert.Equal("test", storage.Data.Tags["environment"]);
        Assert.Equal("platform-team", storage.Data.Tags["owner"]);
    }

    [Fact]
    public async Task Query_NonExistentAccount_ShouldReturnEmpty()
    {
        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;

        var accounts = rg.GetStorageAccounts().ToList();
        Assert.DoesNotContain(accounts, a => a.Data.Name == "st-does-not-exist");
    }

    private static void RunAzCli(string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "az",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.StartInfo.Environment["AZURE_CORE_INSTANCE_DISCOVERY"] = "false";

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"az {arguments} failed: {error}");
        }
    }
}
