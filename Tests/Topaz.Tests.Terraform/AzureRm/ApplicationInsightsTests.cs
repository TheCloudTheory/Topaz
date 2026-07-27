namespace Topaz.Tests.Terraform.AzureRm;

public class ApplicationInsightsTests : AzureRmBatchFixture
{
    [Test]
    public void ApplicationInsights_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("insights_name"), Is.EqualTo("tf-rm-insights"));
    }

    [Test]
    public void ApplicationInsights_ApplicationType_IsCorrect()
    {
        Assert.That(GetOutput<string>("insights_application_type"), Is.EqualTo("web").IgnoreCase);
    }

    [Test]
    public void ApplicationInsights_InstrumentationKey_IsPopulated()
    {
        var key = GetOutput<string>("insights_instrumentation_key");
        Assert.That(key, Is.Not.Null.And.Not.Empty);
        Assert.That(Guid.TryParse(key, out _), Is.True);
    }

    [Test]
    public void ApplicationInsights_ConnectionString_ContainsInstrumentationKey()
    {
        var key = GetOutput<string>("insights_instrumentation_key");
        var connectionString = GetOutput<string>("insights_connection_string");
        Assert.That(connectionString, Does.Contain("InstrumentationKey=").And.Contain(key));
    }
}
