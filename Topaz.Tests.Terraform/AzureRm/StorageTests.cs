namespace Topaz.Tests.Terraform.AzureRm;

public class StorageTests : AzureRmBatchFixture
{
    [Test]
    public void StorageAccount_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("stor_account_name"), Is.EqualTo("tfrmstorageacct"));
            Assert.That(GetOutput<string>("stor_primary_blob_endpoint"), Does.Contain("tfrmstorageacct"));
        });
    }

    [Test]
    public void StorageAccount_WithMinimumTlsVersion_IsApplied()
    {
        Assert.That(GetOutput<string>("stor_tls_account_name"), Is.EqualTo("tfrmstoretls"));
    }
}
