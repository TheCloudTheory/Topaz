namespace Topaz.Tests.Terraform.AzureApi;

public class LoadBalancerTests : TopazFixture
{
    [Test]
    public async Task LoadBalancer_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("load_balancer_basic", outputs =>
        {
            Assert.That(outputs["lb_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-rm-lb"));
        });
    }
}
