namespace Topaz.Tests.Terraform.AzureApi;

public class KeyVaultTests : TopazFixture
{
    [Test]
    public async Task KeyVault_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("key_vault_basic", outputs =>
        {
            Assert.That(outputs["vault_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-kv"));
        });
    }
}
