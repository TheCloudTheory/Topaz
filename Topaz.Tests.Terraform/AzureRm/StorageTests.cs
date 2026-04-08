namespace Topaz.Tests.Terraform.AzureRm;

public class StorageTests : TopazFixture
{
    [Test]
    public async Task StorageAccount_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("storage_account_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["account_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tfrmstorageacct"));
                Assert.That(outputs["primary_blob_endpoint"]!["value"]!.GetValue<string>(), Does.Contain("tfrmstorageacct"));
            });
        });
    }

    [Test]
    public async Task StorageAccount_WithMinimumTlsVersion_IsApplied()
    {
        await RunTerraformWithAzureRm("storage_account_tls", outputs =>
        {
            Assert.That(outputs["account_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tfrmstoretls"));
        });
    }
}
