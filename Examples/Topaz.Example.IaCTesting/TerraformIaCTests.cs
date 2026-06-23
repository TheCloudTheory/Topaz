using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Topaz.Identity;
using Xunit;

namespace Topaz.Example.IaCTesting;

[Collection("Topaz")]
public class TerraformIaCTests
{
    private readonly ArmClient _arm;

    public TerraformIaCTests()
    {
        _arm = new ArmClient(
            new AzureLocalCredential(Globals.GlobalAdminId),
            "00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public async Task Apply_ShouldProvisionStorageAccount_WithCorrectSku()
    {
        RunTerraform("init");
        RunTerraform("apply -auto-approve");

        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;
        var storage = (await rg.GetStorageAccountAsync("stiactest")).Value;

        Assert.Equal("Standard_LRS", storage.Data.Sku.Name.ToString());
        Assert.Equal("test", storage.Data.Tags["environment"]);
        Assert.Equal("platform-team", storage.Data.Tags["owner"]);
    }

    [Fact]
    public async Task Destroy_ShouldRemoveStorageAccount()
    {
        RunTerraform("apply -auto-approve");
        RunTerraform("destroy -auto-approve");

        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;

        var accounts = rg.GetStorageAccounts().ToList();
        Assert.DoesNotContain(accounts, a => a.Data.Name == "stiactest");
    }

    private static void RunTerraform(string arguments)
    {
        var workingDir = Path.Combine(
            AppContext.BaseDirectory, "terraform");

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "terraform",
                Arguments = arguments,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"terraform {arguments} failed: {error}");
        }
    }
}
