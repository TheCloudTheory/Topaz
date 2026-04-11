namespace Topaz.Tests.Terraform.AzureRm;

public class ContainerRegistryTests : AzureRmBatchFixture
{
    [Test]
    public void ContainerRegistry_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("acr_login_server"), Does.Contain("tfrmacr01"));
            Assert.That(GetOutput<bool>("acr_admin_enabled"), Is.False);
        });
    }

    [Test]
    public void ContainerRegistry_WithAdminEnabled_AdminCredentialsAreAvailable()
    {
        Assert.That(GetOutput<string>("acr_admin_registry_name"), Is.EqualTo("tfrmacradmin"));
    }
}
