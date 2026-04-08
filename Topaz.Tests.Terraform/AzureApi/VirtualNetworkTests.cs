namespace Topaz.Tests.Terraform.AzureApi;

public class VirtualNetworkTests : TopazFixture
{
    [Test]
    public async Task VirtualNetwork_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("virtual_network_basic", outputs =>
        {
            Assert.That(outputs["vnet_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-vnet"));
        });
    }
}
