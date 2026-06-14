namespace Topaz.Tests.Terraform.AzureRm;

public class LoadBalancerTests : AzureRmBatchFixture
{
    [Test]
    public void LoadBalancer_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("lb_name"), Is.EqualTo("tf-rm-lb"));
            Assert.That(GetOutput<string>("lb_sku"), Is.EqualTo("Standard"));
        });
    }

    [Test]
    public void LoadBalancer_BasicSku_IsCreated()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("lb_basic_name"), Is.EqualTo("tf-rm-lb-basic"));
            Assert.That(GetOutput<string>("lb_basic_sku"), Is.EqualTo("Basic"));
        });
    }

    [Test]
    public void LoadBalancer_WithTags_TagsArePreserved()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("lb_tagged_name"), Is.EqualTo("tf-rm-lb-tagged"));
            Assert.That(GetOutput<string>("lb_tagged_env"), Is.EqualTo("test"));
            Assert.That(GetOutput<string>("lb_tagged_team"), Is.EqualTo("platform"));
        });
    }
}
