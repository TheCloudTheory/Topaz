namespace Topaz.Tests.Terraform.AzureApi;

public class ResourceGroupTests : TopazFixture
{
    [Test]
    public async Task ResourceGroup_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("resource_group_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["resource_group_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-rg"));
                Assert.That(outputs["location"]!["value"]!.GetValue<string>(), Is.EqualTo("westeurope"));
            });
        });
    }

    [Test]
    public async Task ResourceGroup_WithTags_TagsArePreserved()
    {
        await RunTerraformWithAzureApi("resource_group_with_tags", outputs =>
        {
            Assert.That(outputs["resource_group_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-tagged-rg"));
        });
    }
}
