namespace Topaz.Tests.Terraform.AzureRm;

public class StorageTableEntityIsolatedTests : TopazFixture
{
    [Test]
    public async Task StorageTableEntity_Isolated_Succeeds()
    {
        await RunTerraformWithAzureRm("storage_table_entity", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["partition_key"]!["value"]!.GetValue<string>(), Is.EqualTo("pk1"));
                Assert.That(outputs["row_key"]!["value"]!.GetValue<string>(), Is.EqualTo("rk1"));
            });
        });
    }
}
