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

    [Test]
    public void KeyVault_CreateRsaKey_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("kv_rsa_key_name"), Is.EqualTo("tfrm-rsa-key"));
            Assert.That(GetOutput<string>("kv_rsa_key_id"), Does.Contain("tfrm-rsa-key"));
        });
    }
}
