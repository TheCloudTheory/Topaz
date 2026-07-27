namespace Topaz.Tests.Terraform.AzureRm;

public class VirtualMachineTests : AzureRmBatchFixture
{
    [Test]
    public void VirtualMachine_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("vm_name"), Is.EqualTo("tf-rm-vm"));
    }

    [Test]
    public void VirtualMachine_Location_IsCorrect()
    {
        Assert.That(GetOutput<string>("vm_location"), Is.EqualTo("westeurope").IgnoreCase);
    }
}
