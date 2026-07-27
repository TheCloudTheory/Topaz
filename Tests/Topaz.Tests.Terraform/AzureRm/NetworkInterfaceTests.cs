namespace Topaz.Tests.Terraform.AzureRm;

public class NetworkInterfaceTests : AzureRmBatchFixture
{
    [Test]
    public void NetworkInterface_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("nic_name"), Is.EqualTo("tf-rm-nic"));
            Assert.That(GetOutput<string>("nic_location"), Is.EqualTo("westeurope").IgnoreCase);
        });
    }
}
