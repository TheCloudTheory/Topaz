namespace Topaz.Tests.Terraform.AzureRm;

public class VirtualNetworkTests : TopazFixture
{
    [Test]
    public async Task VirtualNetwork_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("virtual_network_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["vnet_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-vnet"));
                Assert.That(outputs["address_space"]!["value"]![0]!.GetValue<string>(), Is.EqualTo("10.0.0.0/16"));
            });
        });
    }

    [Test]
    public async Task VirtualNetwork_WithDnsServers_DnsServersArePreserved()
    {
        await RunTerraformWithAzureRm("virtual_network_dns", outputs =>
        {
            Assert.That(outputs["vnet_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-vnet-dns"));
        });
    }
}
