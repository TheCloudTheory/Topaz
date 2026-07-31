namespace Topaz.Tests.Terraform.AzureRm;

public class ApiManagementNegativeTests : TopazFixture
{
    [Test]
    public void ApiManagement_InvalidName_TerraformApplyFails()
    {
        // Names starting with a digit are rejected by Topaz with 400 Bad Request.
        var ex = Assert.ThrowsAsync<AssertionException>(
            () => RunTerraformWithAzureRm("api_management_invalid_name"));

        Assert.That(ex!.Message, Does.Contain("failed").Or.Contain("Error").Or.Contain("400"));
    }
}
