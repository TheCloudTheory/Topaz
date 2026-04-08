namespace Topaz.Tests.Terraform.AzureRm;

public class KeyVaultTests : TopazFixture
{
    [Test]
    public async Task KeyVault_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("key_vault_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["vault_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tfrm-kv-test"));
                Assert.That(outputs["vault_uri"]!["value"]!.GetValue<string>(), Does.Contain("tfrm-kv-test"));
            });
        });
    }

    [Test]
    public async Task KeyVault_SoftDeleteEnabled_IsReflectedInProperties()
    {
        await RunTerraformWithAzureRm("key_vault_soft_delete", outputs =>
        {
            Assert.That(outputs["vault_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tfrm-kv-sd"));
        });
    }
}
