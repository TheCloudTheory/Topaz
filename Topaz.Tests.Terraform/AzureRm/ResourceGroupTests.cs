namespace Topaz.Tests.Terraform.AzureRm;

public class ResourceGroupTests : AzureRmBatchFixture
{
    [Test]
    public void ResourceGroup_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("rg_basic_name"), Is.EqualTo("tf-rm-rg"));
            Assert.That(GetOutput<string>("rg_basic_location"), Is.EqualTo("westeurope"));
        });
    }

    [Test]
    public void ResourceGroup_WithTags_TagsArePreserved()
    {
        Assert.That(GetOutput<string>("rg_tags_environment"), Is.EqualTo("test"));
    }
}
