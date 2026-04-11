namespace Topaz.Tests.Terraform.AzureRm;

public class ServiceBusTests : AzureRmBatchFixture
{
    [Test]
    public void ServiceBusNamespace_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("sb_ns_namespace_name"), Is.EqualTo("tf-rm-sb-ns"));
    }

    [Test]
    public void ServiceBusQueue_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("sb_q_queue_name"), Is.EqualTo("tf-rm-queue"));
    }

    [Test]
    public void ServiceBusTopic_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("sb_t_topic_name"), Is.EqualTo("tf-rm-topic"));
    }
}
