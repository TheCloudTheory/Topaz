namespace Topaz.Tests.AzurePowerShell;

public class ContainerRegistryTests : PowerShellTestBase
{
    [Test]
    public async Task ContainerRegistry_WhenCreateCommandIsCalled_RegistryShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-acr-create-rg -Location westeurope -Force");

        await RunAzurePowerShellCommand(
            "New-AzContainerRegistry -Name PsAcr01 -ResourceGroupName ps-acr-create-rg " +
            "-Sku Basic -Location westeurope | ConvertTo-Json -Depth 5",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsAcr01"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                    Assert.That(response["SkuName"]!.GetValue<string>(),
                        Is.EqualTo("Basic").IgnoreCase);
                    Assert.That(response["LoginServer"]!.GetValue<string>(),
                        Does.Contain("psacr01").IgnoreCase);
                    Assert.That(response["ProvisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded").IgnoreCase);
                });
            });

        await RunAzurePowerShellCommand(
            "Remove-AzContainerRegistry -Name PsAcr01 -ResourceGroupName ps-acr-create-rg -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-acr-create-rg -Force");
    }

    [Test]
    public async Task ContainerRegistry_WhenGetCommandIsCalled_RegistryShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-acr-get-rg -Location westeurope -Force");
        await RunAzurePowerShellCommand(
            "New-AzContainerRegistry -Name PsAcr02 -ResourceGroupName ps-acr-get-rg " +
            "-Sku Standard -Location westeurope");

        await RunAzurePowerShellCommand(
            "Get-AzContainerRegistry -Name PsAcr02 -ResourceGroupName ps-acr-get-rg | " +
            "ConvertTo-Json -Depth 5",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsAcr02"));
            });

        await RunAzurePowerShellCommand(
            "Remove-AzContainerRegistry -Name PsAcr02 -ResourceGroupName ps-acr-get-rg -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-acr-get-rg -Force");
    }

    [Test]
    public async Task ContainerRegistry_WhenListCommandIsCalled_AllRegistriesShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-acr-list-rg -Location westeurope -Force");
        await RunAzurePowerShellCommand(
            "New-AzContainerRegistry -Name PsAcrListA -ResourceGroupName ps-acr-list-rg " +
            "-Sku Basic -Location westeurope");
        await RunAzurePowerShellCommand(
            "New-AzContainerRegistry -Name PsAcrListB -ResourceGroupName ps-acr-list-rg " +
            "-Sku Basic -Location westeurope");

        await RunAzurePowerShellCommand(
            "Get-AzContainerRegistry -ResourceGroupName ps-acr-list-rg | ConvertTo-Json -Depth 5",
            response =>
            {
                var array = response.AsArray();
                var names = array!
                    .Select(n => n!["Name"]!.GetValue<string>())
                    .ToList();

                Assert.That(names, Does.Contain("PsAcrListA"));
                Assert.That(names, Does.Contain("PsAcrListB"));
            });

        await RunAzurePowerShellCommand(
            "Remove-AzContainerRegistry -Name PsAcrListA -ResourceGroupName ps-acr-list-rg -Force");
        await RunAzurePowerShellCommand(
            "Remove-AzContainerRegistry -Name PsAcrListB -ResourceGroupName ps-acr-list-rg -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-acr-list-rg -Force");
    }

    [Test]
    public async Task ContainerRegistry_WhenAdminUserIsEnabled_AdminUserEnabledShouldBeTrue()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-acr-admin-rg -Location westeurope -Force");
        await RunAzurePowerShellCommand(
            "New-AzContainerRegistry -Name PsAcr03 -ResourceGroupName ps-acr-admin-rg " +
            "-Sku Premium -Location westeurope");

        await RunAzurePowerShellCommand(
            "Update-AzContainerRegistry -Name PsAcr03 -ResourceGroupName ps-acr-admin-rg " +
            "-EnableAdminUser | ConvertTo-Json -Depth 5",
            response =>
            {
                Assert.That(response["AdminUserEnabled"]!.GetValue<bool>(), Is.True);
            });

        await RunAzurePowerShellCommand(
            "Remove-AzContainerRegistry -Name PsAcr03 -ResourceGroupName ps-acr-admin-rg -Force");
        await RunAzurePowerShellCommand("Remove-AzResourceGroup -Name ps-acr-admin-rg -Force");
    }

    [Test]
    public async Task ContainerRegistry_WhenResourceGroupDoesNotExist_RegistryCannotBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzContainerRegistry -Name PsAcrBad -ResourceGroupName ps-acr-nonexistent-rg " +
            "-Sku Basic -Location westeurope -ErrorAction Stop",
            assertion: null,
            exitCode: 1);
    }
}
