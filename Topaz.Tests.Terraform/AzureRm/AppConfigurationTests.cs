namespace Topaz.Tests.Terraform.AzureRm;

public class AppConfigurationTests : AzureRmBatchFixture
{
    [Test]
    public void AppConfiguration_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("appconfig_name"), Is.EqualTo("tf-rm-appconfig"));
    }

    [Test]
    public void AppConfiguration_Endpoint_IsCorrect()
    {
        Assert.That(GetOutput<string>("appconfig_endpoint"),
            Does.Contain("tf-rm-appconfig").And.Contain("azconfig.topaz.local.dev"));
    }

    [Test]
    public void AppConfiguration_Sku_IsCorrect()
    {
        Assert.That(GetOutput<string>("appconfig_sku"), Is.EqualTo("free").IgnoreCase);
    }
}
