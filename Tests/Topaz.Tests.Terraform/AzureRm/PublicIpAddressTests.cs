namespace Topaz.Tests.Terraform.AzureRm;

public class PublicIpAddressTests : AzureRmBatchFixture
{
    [Test]
    public void PublicIpAddress_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("pip_name"), Is.EqualTo("tf-rm-pip"));
            Assert.That(GetOutput<string>("pip_location"), Is.EqualTo("westeurope").IgnoreCase);
        });
    }
}
