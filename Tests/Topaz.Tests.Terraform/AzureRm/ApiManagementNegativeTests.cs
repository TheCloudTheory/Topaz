namespace Topaz.Tests.Terraform.AzureRm;

public class ApiManagementNegativeTests : TopazFixture
{
    [Test]
    public Task ApiManagement_InvalidName_TerraformApplyFails()
        => RunTerraformWithAzureRm("api_management_invalid_name", expectedExitCode: 1);
}
