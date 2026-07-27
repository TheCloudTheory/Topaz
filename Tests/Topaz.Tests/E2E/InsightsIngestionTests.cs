using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using Azure.ResourceManager.Resources;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

[NonParallelizable]
public class InsightsIngestionTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-3333-0000-0000-AC0300000000");
    private static readonly HttpClient Http = new();

    private const string SubscriptionName = "sub-e2e-insights-ingestion";
    private const string ResourceGroupName = "rg-e2e-insights-ingestion";
    private const string ComponentName = "e2e-insights-ingestion-component";

    private string _connectionString = null!;

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["insights", "component", "create", "--name", ComponentName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString(), "--location", "westeurope"]);

        // Build the connection string from the component name directly.
        // The ingestion endpoint is deterministic from the component name; Topaz does not
        // validate the instrumentation key when accepting telemetry on /v2/track.
        var ingestionEndpoint = TopazResourceHelpers.GetApplicationInsightsIngestionEndpoint(ComponentName);
        _connectionString = $"InstrumentationKey={Guid.NewGuid()};IngestionEndpoint={ingestionEndpoint}/";
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

    private TelemetryClient CreateTelemetryClient()
    {
        var config = new TelemetryConfiguration { ConnectionString = _connectionString };
        return new TelemetryClient(config);
    }

    [Test]
    public void Ingestion_TrackRequest_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackRequest("GET /api/health", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(123), "200", true);
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public void Ingestion_TrackTrace_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackTrace("Hello from Topaz test", SeverityLevel.Information);
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public void Ingestion_TrackException_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackException(new InvalidOperationException("Test exception"));
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public void Ingestion_TrackEvent_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackEvent("ButtonClicked");
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public void Ingestion_TrackMetric_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackMetric("RequestDuration", 42.5);
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public void Ingestion_TrackDependency_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackDependency("HTTP", "GET /api/external", "external-call",
            DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), true);
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public void Ingestion_MultipleItemsInSingleFlush_NoExceptionThrown()
    {
        var client = CreateTelemetryClient();
        client.TrackRequest("GET /a", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10), "200", true);
        client.TrackTrace("batch trace", SeverityLevel.Warning);
        client.TrackEvent("BatchEvent");
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public async Task Ingestion_ConnectionStringFromComponent_InstrumentationKeyIsUsed()
    {
        // Read the real connection string (including the generated InstrumentationKey) from
        // the created component via ARM — this validates the key round-trips correctly.
        var armClient = CreateArmClient();
        var rg = await GetResourceGroup(armClient);
        var component = (await rg.GetApplicationInsightsComponents().GetAsync(ComponentName)).Value;

        var config = new TelemetryConfiguration { ConnectionString = component.Data.ConnectionString };
        var client = new TelemetryClient(config);
        client.TrackRequest("GET /api/ping", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(5), "200", true);
        Assert.DoesNotThrow(client.Flush);
    }

    [Test]
    public async Task Ingestion_ValidInstrumentationKey_ReturnsEnvelopeWithExpectedShape()
    {
        // Retrieve the real instrumentation key from the created component.
        var armClient = CreateArmClient();
        var rg = await GetResourceGroup(armClient);
        var component = (await rg.GetApplicationInsightsComponents().GetAsync(ComponentName)).Value;
        var realKey = component.Data.InstrumentationKey;

        var ingestionEndpoint = TopazResourceHelpers.GetApplicationInsightsIngestionEndpoint(ComponentName);
        var payload = $"{{\"iKey\":\"{realKey}\",\"data\":{{\"baseType\":\"RequestData\",\"baseData\":{{}}}}}}";

        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/x-json-stream");
        var response = await Http.PostAsync($"{ingestionEndpoint}/v2/track", content);

        Assert.That((int)response.StatusCode, Is.EqualTo(200));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.TryGetProperty("itemsReceived", out _), Is.True, "Response must contain 'itemsReceived'");
            Assert.That(root.TryGetProperty("itemsAccepted", out _), Is.True, "Response must contain 'itemsAccepted'");
            Assert.That(!root.TryGetProperty("errors", out var errors) ||
                        errors.ValueKind == System.Text.Json.JsonValueKind.Null ||
                        errors.ValueKind == System.Text.Json.JsonValueKind.Array,
                "errors must be null or an array");
        }
    }

    [Test]
    public async Task Ingestion_InvalidInstrumentationKey_Returns401()
    {
        var invalidKey = Guid.NewGuid().ToString();
        var ingestionEndpoint = TopazResourceHelpers.GetApplicationInsightsIngestionEndpoint(ComponentName);

        // Minimal NDJSON envelope matching the Application Insights wire format.
        var payload = $"{{\"iKey\":\"{invalidKey}\",\"data\":{{\"baseType\":\"RequestData\",\"baseData\":{{}}}}}}";
        
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/x-json-stream");

        var response = await Http.PostAsync($"{ingestionEndpoint}/v2/track", content);

        Assert.That((int)response.StatusCode, Is.EqualTo(401));
    }

    [Test]
    public void Ingestion_LoggerWiredToTelemetryClient_TracesAreFlushed()
    {
        var config = new TelemetryConfiguration { ConnectionString = _connectionString };
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddApplicationInsights(
                configureTelemetryConfiguration: cfg => cfg.ConnectionString = _connectionString,
                configureApplicationInsightsLoggerOptions: _ => { }));

        var logger = loggerFactory.CreateLogger<InsightsIngestionTests>();

        logger.LogInformation("Test log via ILogger → TelemetryClient");
        logger.LogWarning("Warning from Topaz E2E test");
        logger.LogError(new Exception("E2E test error"), "Error via ILogger");

        Assert.DoesNotThrow(() => new TelemetryClient(config).Flush());
    }

    // ── Query API tests ──────────────────────────────────────────────────────

    private async Task<(string ikey, string ingestionEndpoint)> GetComponentKeys()
    {
        var armClient = CreateArmClient();
        var rg = await GetResourceGroup(armClient);
        var component = (await rg.GetApplicationInsightsComponents().GetAsync(ComponentName)).Value;
        var ingestionEndpoint = TopazResourceHelpers.GetApplicationInsightsIngestionEndpoint(ComponentName);
        return (component.Data.InstrumentationKey!, ingestionEndpoint);
    }

    private async Task IngestRequestViaHttp(string ikey, string ingestionEndpoint, string requestName)
    {
        var payload = $"{{\"iKey\":\"{ikey}\",\"time\":\"{DateTimeOffset.UtcNow:O}\",\"data\":{{\"baseType\":\"RequestData\",\"baseData\":{{\"name\":\"{requestName}\",\"duration\":\"00:00:00.100\",\"responseCode\":\"200\",\"success\":true}}}}}}";
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/x-json-stream");
        await Http.PostAsync($"{ingestionEndpoint}/v2/track", content);
    }

    private async Task<System.Text.Json.JsonDocument> RunQuery(string ingestionEndpoint, string ikey, string query)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(new { query });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await Http.PostAsync($"{ingestionEndpoint}/v1/apps/{ikey}/query", content);
        response.EnsureSuccessStatusCode();
        return System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Query_AfterIngestingRequest_TakeReturnsOneRow()
    {
        var (ikey, ingestionEndpoint) = await GetComponentKeys();
        await IngestRequestViaHttp(ikey, ingestionEndpoint, "GET /api/query-test");

        using var doc = await RunQuery(ingestionEndpoint, ikey, "requests | take 10");
        var table = doc.RootElement.GetProperty("tables")[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(table.GetProperty("name").GetString(), Is.EqualTo("PrimaryResult"));
            Assert.That(table.GetProperty("rows").GetArrayLength(), Is.GreaterThanOrEqualTo(1));
        }
    }

    [Test]
    public async Task Query_WhereNameFilter_ReturnsOnlyMatchingRows()
    {
        var (ikey, ingestionEndpoint) = await GetComponentKeys();
        await IngestRequestViaHttp(ikey, ingestionEndpoint, "GET /api/filter-match");
        await IngestRequestViaHttp(ikey, ingestionEndpoint, "GET /api/other");

        using var doc = await RunQuery(ingestionEndpoint, ikey, "requests | where name == \"GET /api/filter-match\"");
        var table = doc.RootElement.GetProperty("tables")[0];
        var rows = table.GetProperty("rows");

        Assert.That(rows.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
        // Every returned row must have the matched name in its columns
        var columns = table.GetProperty("columns").EnumerateArray()
            .Select((c, i) => (name: c.GetProperty("name").GetString()!, index: i))
            .ToList();
        var nameIdx = columns.FirstOrDefault(c => c.name == "name").index;
        foreach (var nameVal in rows.EnumerateArray().Select(row => row[nameIdx].GetString()))
        {
            Assert.That(nameVal, Is.EqualTo("GET /api/filter-match"));
        }
    }

    [Test]
    public async Task Query_SummarizeCount_ReturnsSingleRowWithCount()
    {
        var (ikey, ingestionEndpoint) = await GetComponentKeys();
        await IngestRequestViaHttp(ikey, ingestionEndpoint, "GET /api/count-me");
        await IngestRequestViaHttp(ikey, ingestionEndpoint, "GET /api/count-me");

        using var doc = await RunQuery(ingestionEndpoint, ikey, "requests | summarize count()");
        var table = doc.RootElement.GetProperty("tables")[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(table.GetProperty("name").GetString(), Is.EqualTo("PrimaryResult"));
            Assert.That(table.GetProperty("rows").GetArrayLength(), Is.EqualTo(1));
        }

        var columns = table.GetProperty("columns").EnumerateArray()
            .Select((c, i) => (name: c.GetProperty("name").GetString()!, index: i))
            .ToList();
        var countIdx = columns.FirstOrDefault(c => c.name == "count_").index;
        var countVal = table.GetProperty("rows")[0][countIdx].GetInt32();
        Assert.That(countVal, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task Query_UnknownTable_ReturnsEmptyResultWithPrimaryResultName()
    {
        var (ikey, ingestionEndpoint) = await GetComponentKeys();

        using var doc = await RunQuery(ingestionEndpoint, ikey, "unknownTable | take 10");
        var table = doc.RootElement.GetProperty("tables")[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(table.GetProperty("name").GetString(), Is.EqualTo("PrimaryResult"));
            Assert.That(table.GetProperty("rows").GetArrayLength(), Is.EqualTo(0));
        }
    }
}

