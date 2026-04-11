namespace Topaz.Tests.Terraform.AzureRm;

public class KeyVaultTests : AzureRmBatchFixture
{
    [Test]
    public void KeyVault_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("kv_basic_vault_name"), Is.EqualTo("tfrm-kv-test"));
            Assert.That(GetOutput<string>("kv_basic_vault_uri"), Does.Contain("tfrm-kv-test"));
        });
    }

    [Test]
    public void KeyVault_SoftDeleteEnabled_IsReflectedInProperties()
    {
        Assert.That(GetOutput<string>("kv_sd_vault_name"), Is.EqualTo("tfrm-kv-sd"));
    }
}
