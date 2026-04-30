namespace Topaz.Tests.AzurePowerShell;

public class ResourceGroupTests : PowerShellTestBase
{
    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupIsCreated_ItShouldBeAvailable()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-rg -Location westeurope -Force | Out-Null\n" +
            "Get-AzResourceGroup | Out-Null\n" +
            "Get-AzResourceGroup -Name ps-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-rg -Force | Out-Null");
    }

    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupIsCreatedWithProvidedLocation_TheLocationShouldBeCorrect()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-loc-rg -Location northeurope -Force | Out-Null\n" +
            "$result = Get-AzResourceGroup -Name ps-loc-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzResourceGroup -Name ps-loc-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("northeurope"));
            });
    }

    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupTagsAreUpdated_TheTagsShouldBeReflected()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-tag-rg -Location westeurope -Force | Out-Null\n" +
            "$result = Set-AzResourceGroup -Name ps-tag-rg -Tag @{ env = 'prod'; team = 'platform' } | ConvertTo-Json -Depth 5\n" +
            "Remove-AzResourceGroup -Name ps-tag-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Tags"]!["env"]!.GetValue<string>(), Is.EqualTo("prod"));
                Assert.That(response["Tags"]!["team"]!.GetValue<string>(), Is.EqualTo("platform"));
            });
    }

    [Test]
    public async Task ResourceGroupTests_WhenCheckingExistenceOfCreatedGroup_ItShouldExist()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-exists-rg -Location westeurope -Force | Out-Null\n" +
            "$result = (Get-AzResourceGroup -Name ps-exists-rg -ErrorAction SilentlyContinue) -ne $null | ConvertTo-Json\n" +
            "Remove-AzResourceGroup -Name ps-exists-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task ResourceGroupTests_WhenCheckingExistenceOfNonExistingGroup_ItShouldNotExist()
    {
        await RunAzurePowerShellCommand(
            "(Get-AzResourceGroup -Name ps-non-existing-rg -ErrorAction SilentlyContinue) -ne $null | " +
            "ConvertTo-Json",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.False);
            });
    }
}
