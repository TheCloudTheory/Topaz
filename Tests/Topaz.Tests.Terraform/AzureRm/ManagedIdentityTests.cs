namespace Topaz.Tests.Terraform.AzureRm;

public class ManagedIdentityTests : AzureRmBatchFixture
{
    [Test]
    public void UserAssignedIdentity_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("id_name"), Is.EqualTo("tf-rm-identity"));
            Assert.That(GetOutput<string>("id_client_id"), Is.Not.Empty);
            Assert.That(GetOutput<string>("id_principal_id"), Is.Not.Empty);
        });
    }

    [Test]
    public void UserAssignedIdentity_WithTags_TagsArePreserved()
    {
        Assert.That(GetOutput<string>("id_tags_name"), Is.EqualTo("tf-rm-identity-tagged"));
    }
}
