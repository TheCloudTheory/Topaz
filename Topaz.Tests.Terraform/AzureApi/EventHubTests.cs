namespace Topaz.Tests.Terraform.AzureApi;

public class EventHubTests : TopazFixture
{
    [Test]
    public async Task EventHubNamespace_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("event_hub_namespace", outputs =>
        {
            Assert.That(outputs["namespace_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-eh-ns"));
        });
    }

    [Test]
    public async Task EventHub_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("event_hub", outputs =>
        {
            Assert.That(outputs["eventhub_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-eventhub"));
        });
    }
}
