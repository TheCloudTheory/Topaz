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

    [Test]
    public void StorageTableEntity_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("stor_table_entity_partition_key"), Is.EqualTo("pk1"));
            Assert.That(GetOutput<string>("stor_table_entity_row_key"), Is.EqualTo("rk1"));
        });
    }
}
