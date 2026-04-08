namespace Topaz.Tests.Terraform.AzureRm;

public class ResourceGroupTests : TopazFixture
{
    [Test]
    public async Task ResourceGroup_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("resource_group_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-rg"));
                Assert.That(outputs["location"]!["value"]!.GetValue<string>(), Is.EqualTo("westeurope"));
            });
        });
    }

    [Test]
    public async Task ResourceGroup_WithTags_TagsArePreserved()
    {
        await RunTerraformWithAzureRm("resource_group_with_tags", outputs =>
        {
            Assert.That(outputs["tag_environment"]!["value"]!.GetValue<string>(), Is.EqualTo("test"));
        });
    }
}
