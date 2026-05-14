namespace Topaz.Tests.Terraform.AzureApi;

public class NetworkSecurityGroupTests : TopazFixture
{
    [Test]
    public async Task NetworkSecurityGroup_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("network_security_group_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["nsg_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-nsg"));
                Assert.That(outputs["nsg_provisioning_state"]!["value"]!.GetValue<string>(), Is.EqualTo("Succeeded"));
            });
        });
    }
}
