namespace Topaz.Tests.Terraform.AzureApi;

public class ManagedIdentityTests : TopazFixture
{
    [Test]
    public async Task UserAssignedIdentity_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("managed_identity_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["identity_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-identity"));
                Assert.That(outputs["client_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
            });
        });
    }
}
