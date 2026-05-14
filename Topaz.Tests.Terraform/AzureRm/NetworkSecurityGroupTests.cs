namespace Topaz.Tests.Terraform.AzureRm;

public class NetworkSecurityGroupTests : AzureRmBatchFixture
{
    [Test]
    public void NetworkSecurityGroup_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("nsg_name"), Is.EqualTo("tf-rm-nsg"));
            Assert.That(GetOutput<string>("nsg_location"), Is.EqualTo("westeurope"));
        });
    }

    [Test]
    public void NetworkSecurityGroup_Tags_ArePreserved()
    {
        Assert.That(GetOutput<string>("nsg_tagged_name"), Is.EqualTo("tf-rm-nsg-tagged"));
    }
}
