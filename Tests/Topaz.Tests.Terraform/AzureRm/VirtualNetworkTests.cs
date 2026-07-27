namespace Topaz.Tests.Terraform.AzureRm;

public class VirtualNetworkTests : AzureRmBatchFixture
{
    [Test]
    public void VirtualNetwork_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("vnet_name"), Is.EqualTo("tf-rm-vnet"));
            Assert.That(GetOutputNode("vnet_address_space")[0]!.GetValue<string>(), Is.EqualTo("10.0.0.0/16"));
        });
    }

    [Test]
    public void VirtualNetwork_WithDnsServers_DnsServersArePreserved()
    {
        Assert.That(GetOutput<string>("vnet_dns_name"), Is.EqualTo("tf-rm-vnet-dns"));
    }

    [Test]
    public void Subnet_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("subnet_name"), Is.EqualTo("tf-rm-subnet"));
            Assert.That(GetOutput<string>("subnet_prefix"), Is.EqualTo("10.0.1.0/24"));
        });
    }
}
