namespace Topaz.Tests.Terraform.AzureRm;

public class EventHubTests : TopazFixture
{
    [Test]
    public async Task EventHubNamespace_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("event_hub_namespace", outputs =>
        {
            Assert.That(outputs["namespace_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-eh-ns"));
        });
    }

    [Test]
    public async Task EventHub_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("event_hub", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["eventhub_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-eventhub"));
                Assert.That(outputs["partition_count"]!["value"]!.GetValue<int>(), Is.EqualTo(2));
            });
        });
    }
}
