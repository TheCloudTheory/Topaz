namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class ApiManagementTests : PowerShellTestBase
{
    [Test]
    public async Task ApiManagement_WhenCreateCommandIsCalled_ServiceShouldBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-apim-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzApiManagement -Name ps-apim-create -ResourceGroupName ps-apim-create-rg -Location westeurope -Organization 'Contoso' -AdminEmail 'admin@contoso.com' -Sku Developer | ConvertTo-Json -Depth 5\n" +
            "Remove-AzApiManagement -Name ps-apim-create -ResourceGroupName ps-apim-create-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-apim-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-apim-create"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope").IgnoreCase);
                    Assert.That(response["Sku"]!.GetValue<string>(), Is.EqualTo("Developer").IgnoreCase);
                    Assert.That(response["PublisherEmail"]!.GetValue<string>(), Is.EqualTo("admin@contoso.com").IgnoreCase);
                    Assert.That(response["OrganizationName"]!.GetValue<string>(), Is.EqualTo("Contoso").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task ApiManagement_WhenGetCommandIsCalled_ServiceShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-apim-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzApiManagement -Name ps-apim-get -ResourceGroupName ps-apim-get-rg -Location westeurope -Organization 'Contoso' -AdminEmail 'admin@contoso.com' -Sku Developer | Out-Null\n" +
            "$result = Get-AzApiManagement -Name ps-apim-get -ResourceGroupName ps-apim-get-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzApiManagement -Name ps-apim-get -ResourceGroupName ps-apim-get-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-apim-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-apim-get"));
                    Assert.That(response["Sku"]!.GetValue<string>(), Is.EqualTo("Developer").IgnoreCase);
                    Assert.That(response["PublisherEmail"]!.GetValue<string>(), Is.EqualTo("admin@contoso.com").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task ApiManagement_WhenDeleteCommandIsCalled_ServiceShouldNotBeRetrievable()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-apim-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzApiManagement -Name ps-apim-del -ResourceGroupName ps-apim-del-rg -Location westeurope -Organization 'Contoso' -AdminEmail 'admin@contoso.com' -Sku Developer | Out-Null\n" +
            "Remove-AzApiManagement -Name ps-apim-del -ResourceGroupName ps-apim-del-rg | Out-Null\n" +
            "$result = (Get-AzApiManagement -Name ps-apim-del -ResourceGroupName ps-apim-del-rg -ErrorAction SilentlyContinue) -eq $null\n" +
            "Remove-AzResourceGroup -Name ps-apim-del-rg -Force | Out-Null\n" +
            "$result | ConvertTo-Json",
            response =>
            {
                Assert.That(response.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task ApiManagement_WhenListCommandIsCalled_AllServicesShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-apim-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzApiManagement -Name ps-apim-list-a -ResourceGroupName ps-apim-list-rg -Location westeurope -Organization 'Contoso' -AdminEmail 'admin@contoso.com' -Sku Developer | Out-Null\n" +
            "New-AzApiManagement -Name ps-apim-list-b -ResourceGroupName ps-apim-list-rg -Location westeurope -Organization 'Contoso' -AdminEmail 'admin@contoso.com' -Sku Developer | Out-Null\n" +
            "$result = Get-AzApiManagement -ResourceGroupName ps-apim-list-rg | ConvertTo-Json -Depth 5\n" +
            "Remove-AzApiManagement -Name ps-apim-list-a -ResourceGroupName ps-apim-list-rg | Out-Null\n" +
            "Remove-AzApiManagement -Name ps-apim-list-b -ResourceGroupName ps-apim-list-rg | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-apim-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var array = response.AsArray();
                var names = array!
                    .Select(n => n!["Name"]!.GetValue<string>())
                    .ToList();

                Assert.That(names, Does.Contain("ps-apim-list-a"));
                Assert.That(names, Does.Contain("ps-apim-list-b"));
            });
    }
}
