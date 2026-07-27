namespace Topaz.Tests.Terraform.AzureApi;

public class ServiceBusTests : TopazFixture
{
    [Test]
    public async Task ServiceBusNamespace_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("service_bus_namespace", outputs =>
        {
            Assert.That(outputs["namespace_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-sb-ns"));
        });
    }

    [Test]
    public async Task ServiceBusQueue_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("service_bus_queue", outputs =>
        {
            Assert.That(outputs["queue_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-queue"));
        });
    }

    [Test]
    public async Task ServiceBusTopic_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureApi("service_bus_topic", outputs =>
        {
            Assert.That(outputs["topic_id"]!["value"]!.GetValue<string>(), Does.Contain("tf-api-topic"));
        });
    }
}
