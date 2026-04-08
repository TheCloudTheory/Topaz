namespace Topaz.Tests.Terraform.AzureApi;

public class StorageTests : TopazFixture
{
    [Test]
    public async Task StorageAccount_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("storage_account_basic", outputs =>
        {
            Assert.That(outputs["storage_id"]!["value"]!.GetValue<string>(), Does.Contain("tfapistorageacct"));
        });
    }
}
