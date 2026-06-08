namespace Topaz.Tests.Terraform.AzureRm;

public class DiskTests : AzureRmBatchFixture
{
    [Test]
    public void Disk_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("disk_name"), Is.EqualTo("tf-rm-disk"));
    }

    [Test]
    public void Disk_Sku_IsCorrect()
    {
        Assert.That(GetOutput<string>("disk_sku"), Is.EqualTo("Premium_LRS").IgnoreCase);
    }
}
