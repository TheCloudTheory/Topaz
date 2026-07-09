namespace Topaz.Tests.Terraform.AzureRm;

public class LogAnalyticsTests : AzureRmBatchFixture
{
    [Test]
    public void LogAnalytics_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("loganalytics_name"), Is.EqualTo("tf-rm-loganalytics"));
    }

    [Test]
    public void LogAnalytics_WorkspaceId_IsPopulated()
    {
        var workspaceId = GetOutput<string>("loganalytics_workspace_id");
        Assert.That(workspaceId, Is.Not.Null.And.Not.Empty);
        Assert.That(Guid.TryParse(workspaceId, out _), Is.True);
    }

    [Test]
    public void LogAnalytics_Sku_IsCorrect()
    {
        Assert.That(GetOutput<string>("loganalytics_sku"), Is.EqualTo("PerGB2018").IgnoreCase);
    }

    [Test]
    public void LogAnalytics_RetentionInDays_IsCorrect()
    {
        Assert.That(GetOutput<long>("loganalytics_retention_days"), Is.EqualTo(30));
    }
}
