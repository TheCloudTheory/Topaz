using System.Text.Json.Nodes;

namespace Topaz.Tests.AzureCLI;

public class ApplicationInsightsTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-appinsights";
    private const string ComponentName = "cli-app-insights-query";

    // The ingestion endpoint host is derived from the component name.
    // Port 8899 is the DefaultResourceManagerPort used by all Topaz data-plane endpoints.
    private const string IngestionHost = $"https://{ComponentName}.applicationinsights.topaz.local.dev:8899";

    [Test]
    public async Task AppInsights_WhenComponentIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az monitor app-insights component create --app {ComponentName} -g {ResourceGroup} -l westeurope --kind web",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(ComponentName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("microsoft.insights/components").IgnoreCase);
                    Assert.That(response["properties"]!["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task AppInsights_QueryAfterIngestion_ReturnsPrimaryResultTable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-qry", null, 0);
        await RunAzureCliCommand(
            $"az monitor app-insights component create --app {ComponentName}-qry -g {ResourceGroup}-qry -l westeurope --kind web",
            null, 0);

        // Retrieve the generated instrumentation key.
        string? ikey = null;
        await RunAzureCliCommand(
            $"az monitor app-insights component show --app {ComponentName}-qry -g {ResourceGroup}-qry",
            response =>
            {
                ikey = response["properties"]!["InstrumentationKey"]!.GetValue<string>();
                Assert.That(ikey, Is.Not.Null.And.Not.Empty);
            }, 0);

        var ingestionHost = $"https://{ComponentName}-qry.applicationinsights.topaz.local.dev:8899";

        // Seed one request telemetry envelope directly via the ingestion endpoint.
        var payload = $"{{\\\"iKey\\\":\\\"{ikey}\\\",\\\"time\\\":\\\"{DateTimeOffset.UtcNow:O}\\\",\\\"data\\\":{{\\\"baseType\\\":\\\"RequestData\\\",\\\"baseData\\\":{{\\\"name\\\":\\\"GET /api/cli-test\\\",\\\"responseCode\\\":\\\"200\\\",\\\"success\\\":true}}}}}}";
        await RunAzureCliCommand(
            $"curl -sk -X POST {ingestionHost}/v2/track -H 'Content-Type: application/x-json-stream' -d \"{payload}\"",
            null, 0);

        // Run the query via az rest, targeting the query endpoint on the same host.
        await RunAzureCliCommand(
            $"az rest --method post --url \"{ingestionHost}/v1/apps/{ikey}/query\" --body '{{\"query\":\"requests | take 10\"}}'",
            response =>
            {
                var tables = response["tables"]!.AsArray();
                Assert.Multiple(() =>
                {
                    Assert.That(tables.Count, Is.GreaterThan(0));
                    Assert.That(tables[0]!["name"]!.GetValue<string>(), Is.EqualTo("PrimaryResult"));
                    Assert.That(tables[0]!["rows"]!.AsArray().Count, Is.GreaterThanOrEqualTo(1));
                });
            }, 0);
    }

    [Test]
    public async Task AppInsights_QueryUnknownTable_ReturnsEmptyPrimaryResult()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-empty", null, 0);
        await RunAzureCliCommand(
            $"az monitor app-insights component create --app {ComponentName}-empty -g {ResourceGroup}-empty -l westeurope --kind web",
            null, 0);

        string? ikey = null;
        await RunAzureCliCommand(
            $"az monitor app-insights component show --app {ComponentName}-empty -g {ResourceGroup}-empty",
            response => ikey = response["properties"]!["InstrumentationKey"]!.GetValue<string>(), 0);

        var ingestionHost = $"https://{ComponentName}-empty.applicationinsights.topaz.local.dev:8899";

        await RunAzureCliCommand(
            $"az rest --method post --url \"{ingestionHost}/v1/apps/{ikey}/query\" --body '{{\"query\":\"nonExistentTable | take 10\"}}'",
            response =>
            {
                var tables = response["tables"]!.AsArray();
                Assert.Multiple(() =>
                {
                    Assert.That(tables.Count, Is.GreaterThan(0));
                    Assert.That(tables[0]!["name"]!.GetValue<string>(), Is.EqualTo("PrimaryResult"));
                    Assert.That(tables[0]!["rows"]!.AsArray().Count, Is.EqualTo(0));
                });
            }, 0);
    }
}
