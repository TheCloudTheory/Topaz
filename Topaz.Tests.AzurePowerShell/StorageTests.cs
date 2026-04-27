namespace Topaz.Tests.AzurePowerShell;

public class StorageTests : PowerShellTestBase
{
    [Test]
    public async Task StorageAccount_WhenCreateCommandIsCalled_StorageAccountShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-stor-create-rg -Location westeurope -Force");

        await RunAzurePowerShellCommand(
            "New-AzStorageAccount -ResourceGroupName ps-stor-create-rg -Name psstorsmoke01 " +
            "-Location westeurope -SkuName Standard_LRS | ConvertTo-Json -Depth 5",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["StorageAccountName"]!.GetValue<string>(),
                        Is.EqualTo("psstorsmoke01"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(),
                        Is.EqualTo("ps-stor-create-rg"));
                    Assert.That(response["Location"]!.GetValue<string>(),
                        Is.EqualTo("westeurope"));
                    Assert.That(response["Sku"]!["Name"]!.GetValue<string>(),
                        Is.EqualTo("Standard_LRS").IgnoreCase);
                });
            });

        await RunAzurePowerShellCommand(
            "Remove-AzStorageAccount -ResourceGroupName ps-stor-create-rg -Name psstorsmoke01 -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-stor-create-rg -Force");
    }

    [Test]
    public async Task StorageAccount_WhenGetCommandIsCalled_StorageAccountShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-stor-get-rg -Location westeurope -Force");
        await RunAzurePowerShellCommand(
            "New-AzStorageAccount -ResourceGroupName ps-stor-get-rg -Name psstorsmoke02 " +
            "-Location westeurope -SkuName Standard_LRS");

        await RunAzurePowerShellCommand(
            "Get-AzStorageAccount -ResourceGroupName ps-stor-get-rg -Name psstorsmoke02 | " +
            "ConvertTo-Json -Depth 5",
            response =>
            {
                Assert.That(response["StorageAccountName"]!.GetValue<string>(),
                    Is.EqualTo("psstorsmoke02"));
            });

        await RunAzurePowerShellCommand(
            "Remove-AzStorageAccount -ResourceGroupName ps-stor-get-rg -Name psstorsmoke02 -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-stor-get-rg -Force");
    }

    [Test]
    public async Task StorageAccount_WhenListCommandIsCalled_AllAccountsShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-stor-list-rg -Location westeurope -Force");
        await RunAzurePowerShellCommand(
            "New-AzStorageAccount -ResourceGroupName ps-stor-list-rg -Name psstorlist01a " +
            "-Location westeurope -SkuName Standard_LRS");
        await RunAzurePowerShellCommand(
            "New-AzStorageAccount -ResourceGroupName ps-stor-list-rg -Name psstorlist01b " +
            "-Location westeurope -SkuName Standard_LRS");

        await RunAzurePowerShellCommand(
            "Get-AzStorageAccount -ResourceGroupName ps-stor-list-rg | ConvertTo-Json -Depth 5",
            response =>
            {
                var array = response.AsArray();
                var names = array!
                    .Select(n => n!["StorageAccountName"]!.GetValue<string>())
                    .ToList();

                Assert.That(names, Does.Contain("psstorlist01a"));
                Assert.That(names, Does.Contain("psstorlist01b"));
            });

        await RunAzurePowerShellCommand(
            "Remove-AzStorageAccount -ResourceGroupName ps-stor-list-rg -Name psstorlist01a -Force");
        await RunAzurePowerShellCommand(
            "Remove-AzStorageAccount -ResourceGroupName ps-stor-list-rg -Name psstorlist01b -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-stor-list-rg -Force");
    }

    [Test]
    public async Task StorageAccount_WhenTagsAreApplied_TagsShouldBeReflected()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-stor-tags-rg -Location westeurope -Force");
        await RunAzurePowerShellCommand(
            "New-AzStorageAccount -ResourceGroupName ps-stor-tags-rg -Name psstortags01 " +
            "-Location westeurope -SkuName Standard_LRS");

        await RunAzurePowerShellCommand(
            "Set-AzStorageAccount -ResourceGroupName ps-stor-tags-rg -Name psstortags01 " +
            "-Tag @{ env = 'staging'; team = 'infra' } | ConvertTo-Json -Depth 5",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Tags"]!["env"]!.GetValue<string>(), Is.EqualTo("staging"));
                    Assert.That(response["Tags"]!["team"]!.GetValue<string>(), Is.EqualTo("infra"));
                });
            });

        await RunAzurePowerShellCommand(
            "Remove-AzStorageAccount -ResourceGroupName ps-stor-tags-rg -Name psstortags01 -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-stor-tags-rg -Force");
    }

    [Test]
    public async Task StorageAccount_WhenResourceGroupDoesNotExist_StorageAccountCannotBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzStorageAccount -ResourceGroupName ps-stor-nonexistent-rg -Name psstorbad01 " +
            "-Location westeurope -SkuName Standard_LRS -ErrorAction Stop",
            assertion: null,
            exitCode: 1);
    }
}
