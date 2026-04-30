namespace Topaz.Tests.AzurePowerShell;

public class KeyVaultTests : PowerShellTestBase
{
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalled_KeyVaultShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzKeyVault -Name PsTestVault01 -ResourceGroupName ps-kv-rg -Location westeurope | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVault -VaultName PsTestVault01 -ResourceGroupName ps-kv-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["VaultName"]!.GetValue<string>(),
                        Is.EqualTo("PsTestVault01"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(),
                        Is.EqualTo("ps-kv-rg"));
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope"));
                    Assert.That(response["Sku"]!.GetValue<string>(),
                        Is.EqualTo("Standard").IgnoreCase);
                    Assert.That(response["EnableSoftDelete"]!.GetValue<bool>(), Is.True);
                    Assert.That(response["SoftDeleteRetentionInDays"]!.GetValue<int>(),
                        Is.EqualTo(90));
                    Assert.That(response["EnableRbacAuthorization"]!.GetValue<bool>(), Is.True);
                });
            });
    }

    [Test]
    public async Task KeyVaultTests_WhenGetCommandIsCalled_KeyVaultShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzKeyVault -Name PsGetVault02 -ResourceGroupName ps-kv-get-rg -Location westeurope | Out-Null\n" +
            "$result = Get-AzKeyVault -VaultName PsGetVault02 | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVault -VaultName PsGetVault02 -ResourceGroupName ps-kv-get-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["VaultName"]!.GetValue<string>(), Is.EqualTo("PsGetVault02"));
            });
    }

    [Test]
    public async Task KeyVaultTests_WhenListCommandIsCalled_AllKeyVaultsShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzKeyVault -Name PsListVaultA -ResourceGroupName ps-kv-list-rg -Location westeurope | Out-Null\n" +
            "New-AzKeyVault -Name PsListVaultB -ResourceGroupName ps-kv-list-rg -Location westeurope | Out-Null\n" +
            "$result = Get-AzKeyVault | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVault -VaultName PsListVaultA -ResourceGroupName ps-kv-list-rg -Force | Out-Null\n" +
            "Remove-AzKeyVault -VaultName PsListVaultB -ResourceGroupName ps-kv-list-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                // ConvertTo-Json wraps an array as a JSON array
                var array = response.AsArray();
                var vaultNames = array!
                    .Select(n => n!["VaultName"]!.GetValue<string>())
                    .ToList();

                Assert.That(vaultNames, Does.Contain("PsListVaultA"));
                Assert.That(vaultNames, Does.Contain("PsListVaultB"));
            });
    }

    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithPremiumSku_SkuShouldBeSet()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-sku-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzKeyVault -Name PsSkuVault03 -ResourceGroupName ps-kv-sku-rg -Location westeurope -Sku Premium | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVault -VaultName PsSkuVault03 -ResourceGroupName ps-kv-sku-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-sku-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Sku"]!.GetValue<string>(),
                    Is.EqualTo("Premium").IgnoreCase);
            });
    }

    [Test]
    public async Task KeyVaultTests_WhenResourceGroupDoesNotExist_KeyVaultCannotBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzKeyVault -Name PsBadVault -ResourceGroupName ps-nonexistent-rg " +
            "-Location westeurope -ErrorAction Stop",
            assertion: null,
            exitCode: 1);
    }
}
