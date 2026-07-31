namespace Topaz.Tests.Terraform.AzureRm;

public class ApiManagementTests : AzureRmBatchFixture
{
    [Test]
    public void ApiManagement_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("apim_basic_name"), Is.EqualTo("tf-rm-apim"));
            Assert.That(GetOutput<string>("apim_basic_publisher_name"), Is.EqualTo("Topaz Tests"));
            Assert.That(GetOutput<string>("apim_basic_publisher_email"), Is.EqualTo("admin@topaz.local.dev"));
        });
    }

    [Test]
    public void ApiManagement_WithTags_TagsAreApplied()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("apim_tagged_name"), Is.EqualTo("tf-rm-apim-tagged"));
            Assert.That(GetOutput<string>("apim_tagged_env"), Is.EqualTo("test"));
            Assert.That(GetOutput<string>("apim_tagged_team"), Is.EqualTo("platform"));
        });
    }
}
