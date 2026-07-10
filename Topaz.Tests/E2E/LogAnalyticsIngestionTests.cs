using System.Net;
using System.Text;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.OperationalInsights;
using Azure.ResourceManager.OperationalInsights.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class LogAnalyticsIngestionTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-2222-0000-0000-AC0300000000");

    private const string SubscriptionName = "sub-e2e-la-ingestion";
    private const string ResourceGroupName = "rg-e2e-la-ingestion";

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
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-la-ingestion-ws",
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

    private HttpClient CreateIngestionClient()
    {
        return new HttpClient()
        {
            BaseAddress = new Uri($"https://{_workspaceCustomerId}.ods.opinsights.topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}")
        };
    }

    private static StringContent LogRecordsPayload(object records) =>
        new(System.Text.Json.JsonSerializer.Serialize(records), Encoding.UTF8, "application/json");

    [Test]
    public async Task LogIngestion_Post_Returns200WithEmptyBody()
    {
        using var http = CreateIngestionClient();
        var payload = LogRecordsPayload(new[] { new { Message = "hello", Severity = "Info" } });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs?api-version=2016-04-01");
        request.Content = payload;
        request.Headers.Add("Log-Type", "MyCustomLog");

        var response = await http.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentLength ?? 0, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task LogIngestion_Post_MultipleRecords_Returns200()
    {
        using var http = CreateIngestionClient();
        var records = new[]
        {
            new { Message = "record-1", Level = "Warning" },
            new { Message = "record-2", Level = "Error" },
            new { Message = "record-3", Level = "Info" }
        };
        var payload = LogRecordsPayload(records);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs?api-version=2016-04-01");
        request.Content = payload;
        request.Headers.Add("Log-Type", "MyBatchLog");

        var response = await http.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task LogIngestion_Post_DifferentLogTypes_EachReturns200()
    {
        using var http = CreateIngestionClient();

        foreach (var logType in new[] { "AppTraces", "AppMetrics", "CustomEvents" })
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs?api-version=2016-04-01");
            request.Content = LogRecordsPayload(new[] { new { Source = logType } });
            request.Headers.Add("Log-Type", logType);

            var response = await http.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Log-Type: {logType}");
        }
    }
}
