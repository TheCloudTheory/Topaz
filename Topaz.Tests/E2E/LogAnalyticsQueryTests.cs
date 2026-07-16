using System.Net;
using System.Text;
using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using Azure.ResourceManager;
using Azure.ResourceManager.OperationalInsights;
using Azure.ResourceManager.OperationalInsights.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class LogAnalyticsQueryTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-3333-0000-0000-AC0300000000");

    private const string SubscriptionName = "sub-e2e-la-query";
    private const string ResourceGroupName = "rg-e2e-la-query";
    private const string WorkspaceName = "e2e-la-query-ws";

    private string _workspaceCustomerId = null!;

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var client = CreateArmClient();
        var rg = await GetResourceGroup(client);
        var result = await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, WorkspaceName,
                new OperationalInsightsWorkspaceData(AzureLocation.WestEurope)
                {
                    Sku = new OperationalInsightsWorkspaceSku(OperationalInsightsWorkspaceSkuName.PerGB2018),
                    RetentionInDays = 30
                });
        _workspaceCustomerId = result.Value.Data.CustomerId!.ToString()!;
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private async Task<ResourceGroupResource> GetResourceGroup(ArmClient client)
    {
        var sub = await client.GetDefaultSubscriptionAsync();
        return (await sub.GetResourceGroupAsync(ResourceGroupName)).Value;
    }

    private LogsQueryClient CreateQueryClient() =>
        new(new Uri(TopazResourceHelpers.GetLogAnalyticsQueryEndpoint()),
            new AzureLocalCredential(Globals.GlobalAdminId));

    private HttpClient CreateIngestionClient() =>
        new() { BaseAddress = new Uri(TopazResourceHelpers.GetLogAnalyticsIngestionEndpoint(_workspaceCustomerId)) };

    private async Task IngestRecords(string logType, params object[] records)
    {
        using var http = CreateIngestionClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs?api-version=2016-04-01");
        request.Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(records), Encoding.UTF8, "application/json");
        request.Headers.Add("Log-Type", logType);
        var response = await http.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task LogAnalyticsQuery_CustomTable_ReturnsPrimaryResultTable()
    {
        await IngestRecords("QueryResult", new { Message = "hello-query", Severity = "Info" });

        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(_workspaceCustomerId, "QueryResult_CL | take 10", QueryTimeRange.All);

        Assert.That(result.Value.Table.Name, Is.EqualTo("PrimaryResult"));
    }

    [Test]
    public async Task LogAnalyticsQuery_CustomTable_ReturnsIngestedRows()
    {
        await IngestRecords("IngestedRows", new { Message = "row1" }, new { Message = "row2" });

        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(_workspaceCustomerId, "IngestedRows_CL | take 10", QueryTimeRange.All);

        Assert.That(result.Value.Table.Rows, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task LogAnalyticsQuery_AzureActivity_ReturnsEmptyResult()
    {
        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(_workspaceCustomerId, "AzureActivity | take 10", QueryTimeRange.All);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Table.Name, Is.EqualTo("PrimaryResult"));
            Assert.That(result.Value.Table.Rows, Is.Empty);
        }
    }

    [Test]
    public async Task LogAnalyticsQuery_AzureDiagnostics_ReturnsEmptyResult()
    {
        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(_workspaceCustomerId, "AzureDiagnostics | take 10", QueryTimeRange.All);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Table.Name, Is.EqualTo("PrimaryResult"));
            Assert.That(result.Value.Table.Rows, Is.Empty);
        }
    }

    [Test]
    public async Task LogAnalyticsQuery_TakeOperator_LimitsReturnedRows()
    {
        for (var i = 0; i < 5; i++)
            await IngestRecords("TakeLimitTable", new { Index = i });

        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(_workspaceCustomerId, "TakeLimitTable_CL | take 2", QueryTimeRange.All);

        Assert.That(result.Value.Table.Rows, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task LogAnalyticsQuery_WhereFilter_ReturnsOnlyMatchingRows()
    {
        await IngestRecords("WhereFilterTable",
            new { Message = "keep-me", Level = "Error" },
            new { Message = "skip-me", Level = "Info" });

        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(
            _workspaceCustomerId,
            "WhereFilterTable_CL | where Level_s == \"Error\"",
            QueryTimeRange.All);

        Assert.That(result.Value.Table.Rows, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task LogAnalyticsQuery_ProjectOperator_ReturnsOnlySelectedColumns()
    {
        await IngestRecords("ProjectColTable", new { KeepCol = "foo", DropCol = "bar" });

        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(
            _workspaceCustomerId,
            "ProjectColTable_CL | project KeepCol_s",
            QueryTimeRange.All);

        var columns = result.Value.Table.Columns.Select(c => c.Name).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(columns, Does.Contain("KeepCol_s"));
            Assert.That(columns, Does.Not.Contain("DropCol_s"));
        }
    }

    [Test]
    public async Task LogAnalyticsQuery_SummarizeCount_ReturnsOneRowPerGroup()
    {
        await IngestRecords("SummarizeTable",
            new { Category = "A" },
            new { Category = "A" },
            new { Category = "B" });

        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(
            _workspaceCustomerId,
            "SummarizeTable_CL | summarize count() by Category_s",
            QueryTimeRange.All);

        Assert.That(result.Value.Table.Rows, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task LogAnalyticsQuery_UnknownTable_ReturnsEmptyResult()
    {
        var client = CreateQueryClient();
        var result = await client.QueryWorkspaceAsync(_workspaceCustomerId, "NoSuchTable_CL | take 10", QueryTimeRange.All);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Table.Name, Is.EqualTo("PrimaryResult"));
            Assert.That(result.Value.Table.Rows, Is.Empty);
        }
    }
}
