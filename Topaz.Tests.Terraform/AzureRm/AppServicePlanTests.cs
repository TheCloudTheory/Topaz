namespace Topaz.Tests.Terraform.AzureRm;

public class AppServicePlanTests : AzureRmBatchFixture
{
    [Test]
    public void AppServicePlan_BasicTier_CreatedWithCorrectName()
    {
        Assert.That(GetOutput<string>("asp_basic_name"), Is.EqualTo("tf-rm-asp-basic"));
    }

    [Test]
    public void AppServicePlan_BasicTier_HasCorrectSku()
    {
        Assert.That(GetOutput<string>("asp_basic_sku"), Is.EqualTo("B1"));
    }

    [Test]
    public void AppServicePlan_StandardTier_CreatedWithCorrectName()
    {
        Assert.That(GetOutput<string>("asp_standard_name"), Is.EqualTo("tf-rm-asp-standard"));
    }

    [Test]
    public void AppServicePlan_StandardTier_HasCorrectSku()
    {
        Assert.That(GetOutput<string>("asp_standard_sku"), Is.EqualTo("S1"));
    }
}
