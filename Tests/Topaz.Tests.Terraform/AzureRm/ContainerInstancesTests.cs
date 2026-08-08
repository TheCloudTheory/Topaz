namespace Topaz.Tests.Terraform.AzureRm;

public class ContainerInstancesTests : AzureRmBatchFixture
{
    [Test]
    public void ContainerInstances_BasicGroup_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("aci_basic_name"), Is.EqualTo("tf-rm-aci-basic"));
            Assert.That(GetOutput<string>("aci_basic_location"), Is.EqualTo("westeurope"));
            Assert.That(GetOutput<string>("aci_basic_os_type"), Is.EqualTo("Linux"));
            Assert.That(GetOutput<string>("aci_basic_restart_policy"), Is.EqualTo("Always"));
        });
    }

    [Test]
    public void ContainerInstances_PublicGroup_HasPublicIpAndDnsLabel()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("aci_public_name"), Is.EqualTo("tf-rm-aci-public"));
            Assert.That(GetOutput<string>("aci_public_ip_type"), Is.EqualTo("Public"));
            Assert.That(GetOutput<string>("aci_public_dns_label"), Is.EqualTo("tf-rm-aci-public"));
        });
    }

    [Test]
    public void ContainerInstances_TaggedGroup_TagsArePreserved()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("aci_tagged_name"), Is.EqualTo("tf-rm-aci-tagged"));
            Assert.That(GetOutput<string>("aci_tagged_env"), Is.EqualTo("test"));
            Assert.That(GetOutput<string>("aci_tagged_team"), Is.EqualTo("platform"));
        });
    }
}
