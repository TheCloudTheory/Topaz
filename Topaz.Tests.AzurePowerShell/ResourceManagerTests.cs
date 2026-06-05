namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class ResourceManagerTests : PowerShellTestBase
{
    // Build the template as a PowerShell hashtable to avoid JSON quoting issues in C# string literals.
    private const string BuildCancelTemplate =
        "$tmpl = @{ '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#'; " +
        "contentVersion = '1.0.0.0'; " +
        "resources = @(" +
        "@{ type = 'Microsoft.ManagedIdentity/userAssignedIdentities'; apiVersion = '2023-01-31'; name = 'ps-cancel-01'; location = 'westeurope' }," +
        "@{ type = 'Microsoft.ManagedIdentity/userAssignedIdentities'; apiVersion = '2023-01-31'; name = 'ps-cancel-02'; location = 'westeurope' }," +
        "@{ type = 'Microsoft.ManagedIdentity/userAssignedIdentities'; apiVersion = '2023-01-31'; name = 'ps-cancel-03'; location = 'westeurope' }" +
        ") }";

    [Test]
    public async Task ResourceManagerTests_WhenCancellingRunningDeployment_ItShouldSucceed()
    {
        // New-AzResourceGroupDeployment with -AsJob starts the deployment in the background.
        // Stop-AzResourceGroupDeployment cancels it; succeeds whether it is still running or already completed.
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-rg-cancel -Location westeurope -Force | Out-Null\n" +
            BuildCancelTemplate + "\n" +
            "$job = New-AzResourceGroupDeployment -Name ps-cancel-dep -ResourceGroupName ps-rg-cancel -TemplateObject $tmpl -AsJob\n" +
            "Start-Sleep -Milliseconds 100\n" +
            "Stop-AzResourceGroupDeployment -Name ps-cancel-dep -ResourceGroupName ps-rg-cancel | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-rg-cancel -Force | Out-Null");
    }

    [Test]
    public async Task ResourceManagerTests_WhenCancellingRunningSubscriptionScopeDeployment_ItShouldSucceed()
    {
        // New-AzDeployment (subscription scope) with -AsJob, then Stop-AzDeployment.
        await RunAzurePowerShellCommand(
            BuildCancelTemplate + "\n" +
            "$job = New-AzDeployment -Name ps-sub-cancel-dep -Location westeurope -TemplateObject $tmpl -AsJob\n" +
            "Start-Sleep -Milliseconds 100\n" +
            "Stop-AzDeployment -Name ps-sub-cancel-dep | Out-Null\n" +
            "Remove-AzDeployment -Name ps-sub-cancel-dep | Out-Null");
    }
}
