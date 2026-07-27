namespace Topaz.Tests.AzureCLI;

public class LogAnalyticsTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-loganalytics";
    private const string WorkspaceName = "my-cli-workspace";

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName} -g {ResourceGroup} -l westeurope",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(WorkspaceName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.OperationalInsights/workspaces").IgnoreCase);
                    Assert.That(response["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsCreated_CustomerIdShouldBePopulated()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-ids", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName}-ids -g {ResourceGroup}-ids -l westeurope",
            response =>
            {
                var customerId = response["customerId"]?.GetValue<string>();
                Assert.That(customerId, Is.Not.Null.And.Not.Empty);
            }, 0);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName}-del -g {ResourceGroup}-del -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace delete -n {WorkspaceName}-del -g {ResourceGroup}-del --yes --force",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace show -n {WorkspaceName}-del -g {ResourceGroup}-del",
            null, 3);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspacesAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName}-list-a -g {ResourceGroup}-list -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName}-list-b -g {ResourceGroup}-list -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace list -g {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{WorkspaceName}-list-a"));
                    Assert.That(names, Does.Contain($"{WorkspaceName}-list-b"));
                });
            }, 0);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsUpdatedWithRetention_RetentionShouldPersist()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-update", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName}-update -g {ResourceGroup}-update -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace update -n {WorkspaceName}-update -g {ResourceGroup}-update --retention-time 60",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace show -n {WorkspaceName}-update -g {ResourceGroup}-update",
            response =>
            {
                Assert.That(response["retentionInDays"]!.GetValue<int>(), Is.EqualTo(60));
            }, 0);
    }

    [Test]
    public async Task LogAnalytics_WhenWorkspaceIsUpdatedWithTags_TagsShouldPersist()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-tags", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {WorkspaceName}-tags -g {ResourceGroup}-tags -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace update -n {WorkspaceName}-tags -g {ResourceGroup}-tags --tags env=test",
            null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace show -n {WorkspaceName}-tags -g {ResourceGroup}-tags",
            response =>
            {
                Assert.That(response["tags"]!["env"]!.GetValue<string>(), Is.EqualTo("test"));
            }, 0);
    }
}
