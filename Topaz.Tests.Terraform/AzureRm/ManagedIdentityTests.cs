namespace Topaz.Tests.Terraform.AzureRm;

public class ManagedIdentityTests : TopazFixture
{
    [Test]
    public async Task UserAssignedIdentity_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("managed_identity_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["identity_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-identity"));
                Assert.That(outputs["client_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
                Assert.That(outputs["principal_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
            });
        });
    }

    [Test]
    public async Task UserAssignedIdentity_WithTags_TagsArePreserved()
    {
        await RunTerraformWithAzureRm("managed_identity_with_tags", outputs =>
        {
            Assert.That(outputs["identity_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-identity-tagged"));
        });
    }
}
