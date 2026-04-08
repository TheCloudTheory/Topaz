namespace Topaz.Tests.Terraform.AzureRm;

public class ContainerRegistryTests : TopazFixture
{
    [Test]
    public async Task ContainerRegistry_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("container_registry_basic", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["login_server"]!["value"]!.GetValue<string>(), Does.Contain("tfrmacr01"));
                Assert.That(outputs["admin_enabled"]!["value"]!.GetValue<bool>(), Is.False);
            });
        });
    }

    [Test]
    public async Task ContainerRegistry_WithAdminEnabled_AdminCredentialsAreAvailable()
    {
        await RunTerraformWithAzureRm("container_registry_admin_enabled", outputs =>
        {
            Assert.That(outputs["registry_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tfrmacradmin"));
        });
    }
}
