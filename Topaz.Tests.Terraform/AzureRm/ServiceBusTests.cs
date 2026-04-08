namespace Topaz.Tests.Terraform.AzureRm;

public class ServiceBusTests : TopazFixture
{
    [Test]
    public async Task ServiceBusNamespace_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("service_bus_namespace", outputs =>
        {
            Assert.That(outputs["namespace_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-sb-ns"));
        });
    }

    [Test]
    public async Task ServiceBusQueue_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("service_bus_queue", outputs =>
        {
            Assert.That(outputs["queue_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-queue"));
        });
    }

    [Test]
    public async Task ServiceBusTopic_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureRm("service_bus_topic", outputs =>
        {
            Assert.That(outputs["topic_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-rm-topic"));
        });
    }
}
