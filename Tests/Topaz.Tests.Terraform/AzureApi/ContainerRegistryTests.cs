namespace Topaz.Tests.Terraform.AzureApi;

public class ContainerRegistryTests : TopazFixture
{
    [Test]
    public async Task ContainerRegistry_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("container_registry_basic", outputs =>
        {
            Assert.That(outputs["registry_id"]!["value"]!.GetValue<string>(), Does.Contain("tfapiacr01"));
        });
    }
}
