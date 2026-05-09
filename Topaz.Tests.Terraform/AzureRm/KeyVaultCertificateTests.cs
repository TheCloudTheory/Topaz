namespace Topaz.Tests.Terraform.AzureRm;

public class KeyVaultCertificateTests : AzureRmBatchFixture
{
    [Test]
    public void KeyVaultCertificate_Create_Succeeds()
    {
        Assert.That(GetOutput<string>("kv_cert_name"), Is.EqualTo("tfrm-kv-cert"));
    }

    [Test]
    public void KeyVaultCertificate_SecretId_ContainsCertName()
    {
        Assert.That(GetOutput<string>("kv_cert_secret_id"), Does.Contain("tfrm-kv-cert"));
    }

    [Test]
    public void KeyVaultCertificate_Thumbprint_IsNotEmpty()
    {
        Assert.That(GetOutput<string>("kv_cert_thumbprint"), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void KeyVaultCertificate_Version_IsNotEmpty()
    {
        Assert.That(GetOutput<string>("kv_cert_version"), Is.Not.Null.And.Not.Empty);
    }
}
