namespace Topaz.Tests.Terraform.AzureRm;

public class AvailabilitySetTests : AzureRmBatchFixture
{
    [Test]
    public void AvailabilitySet_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("avset_name"), Is.EqualTo("tf-rm-avset"));
    }

    [Test]
    public void AvailabilitySet_Location_IsCorrect()
    {
        Assert.That(GetOutput<string>("avset_location"), Is.EqualTo("westeurope").IgnoreCase);
    }

    [Test]
    public void AvailabilitySet_PlatformFaultDomainCount_IsCorrect()
    {
        Assert.That(GetOutput<int>("avset_platform_fault_domain_count"), Is.EqualTo(2));
    }

    [Test]
    public void AvailabilitySet_PlatformUpdateDomainCount_IsCorrect()
    {
        Assert.That(GetOutput<int>("avset_platform_update_domain_count"), Is.EqualTo(5));
    }
}
