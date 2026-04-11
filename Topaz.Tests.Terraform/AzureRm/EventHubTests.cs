namespace Topaz.Tests.Terraform.AzureRm;

public class EventHubTests : AzureRmBatchFixture
{
    [Test]
    public void EventHubNamespace_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("eh_ns_namespace_name"), Is.EqualTo("tf-rm-eh-ns"));
    }

    [Test]
    public void EventHub_CreateAndDestroy_Succeeds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetOutput<string>("ehub_name"), Is.EqualTo("tf-rm-eventhub"));
            Assert.That(GetOutput<int>("ehub_partition_count"), Is.EqualTo(2));
        });
    }
}
