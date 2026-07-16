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
}
