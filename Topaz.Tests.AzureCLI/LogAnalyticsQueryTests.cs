namespace Topaz.Tests.AzureCLI;

public class LogAnalyticsQueryTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-la-query";
    private const string WorkspaceName = "cli-la-query-ws";

    // Each test gets its own suffix so tests are fully independent.
    // Ingestion uses the legacy Data Collector API (curl POST) since
    // `az monitor log-analytics` has no ingestion sub-command.

    private async Task CreateWorkspace(string suffix = "")
    {
        var rg = $"{ResourceGroup}{suffix}";
        var ws = $"{WorkspaceName}{suffix}";
        await RunAzureCliCommand($"az group create -l westeurope -n {rg}", null, 0);
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace create -n {ws} -g {rg} -l westeurope",
            null, 0);
    }

    private async Task<string> GetCustomerId(string suffix = "")
    {
        var rg = $"{ResourceGroup}{suffix}";
        var ws = $"{WorkspaceName}{suffix}";
        var customerId = "";
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace show -n {ws} -g {rg} --query customerId -o tsv");

        // Use JSON output to extract customerId reliably
        await RunAzureCliCommand(
            $"az monitor log-analytics workspace show -n {ws} -g {rg}",
            response =>
            {
                customerId = response["customerId"]!.GetValue<string>();
            }, 0);

        return customerId;
    }

    /// <summary>
    /// Ingests records into a workspace via the legacy Data Collector API (ODS endpoint).
    /// Uses curl inside the CLI container, so no extra tooling is needed.
    /// </summary>
    private async Task IngestViaCurl(string customerId, string logType, string jsonBody)
    {
        var host = $"{customerId}.ods.opinsights.topaz.local.dev";
        var mappingResult = await AzureCliContainer.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            $"grep -qF '{host}' /etc/hosts || echo '{TopazContainerIpAddress} {host}' >> /etc/hosts"
        });
        
        Assert.That(mappingResult.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {host}. STDERR: {mappingResult.Stderr}");

        var endpoint = $"https://{host}:8899/api/logs?api-version=2016-04-01";
        await TryRunAzureCliCommand(
            $"curl -k -s -o /dev/null -w '%{{http_code}}' -X POST \"{endpoint}\" " +
            $"-H 'Content-Type: application/json' " +
            $"-H 'Log-Type: {logType}' " +
            $"--data '{jsonBody}'");
    }

    [Test]
    public async Task LogAnalyticsQuery_AzureActivity_ReturnsEmptyTable()
    {
        await CreateWorkspace("-az-activity");
        var customerId = await GetCustomerId("-az-activity");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"AzureActivity | take 10\"",
            response =>
            {
                var tables = response.AsArray();
                Assert.That(tables, Is.Not.Null);
                Assert.That(tables!.Count, Is.GreaterThanOrEqualTo(1));
                var primaryTable = tables![0]!;
                Assert.That(primaryTable["name"]!.GetValue<string>(), Is.EqualTo("PrimaryResult"));
                Assert.That(primaryTable["rows"]!.AsArray(), Is.Empty);
            }, 0);
    }

    [Test]
    public async Task LogAnalyticsQuery_AzureDiagnostics_ReturnsEmptyTable()
    {
        await CreateWorkspace("-az-diag");
        var customerId = await GetCustomerId("-az-diag");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"AzureDiagnostics | take 10\"",
            response =>
            {
                var tables = response.AsArray();
                Assert.That(tables, Is.Not.Null);
                var primaryTable = tables![0]!;
                Assert.That(primaryTable["name"]!.GetValue<string>(), Is.EqualTo("PrimaryResult"));
                Assert.That(primaryTable["rows"]!.AsArray(), Is.Empty);
            }, 0);
    }

    [Test]
    public async Task LogAnalyticsQuery_CustomTable_ReturnsPrimaryResult()
    {
        await CreateWorkspace("-custom");
        var customerId = await GetCustomerId("-custom");
        await IngestViaCurl(customerId, "CliQueryResult", "[{\"Message\":\"cli-hello\",\"Severity\":\"Info\"}]");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"CliQueryResult_CL | take 10\"",
            response =>
            {
                var tables = response.AsArray();
                Assert.That(tables, Is.Not.Null);
                Assert.That(tables![0]!["name"]!.GetValue<string>(), Is.EqualTo("PrimaryResult"));
            }, 0);
    }

    [Test]
    public async Task LogAnalyticsQuery_CustomTable_ReturnsIngestedRows()
    {
        await CreateWorkspace("-rows");
        var customerId = await GetCustomerId("-rows");
        await IngestViaCurl(customerId, "CliRowsTable",
            "[{\"Message\":\"row1\"},{\"Message\":\"row2\"}]");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"CliRowsTable_CL | take 10\"",
            response =>
            {
                var tables = response.AsArray();
                var rows = tables![0]!["rows"]!.AsArray();
                Assert.That(rows!.Count, Is.GreaterThanOrEqualTo(2));
            }, 0);
    }

    [Test]
    public async Task LogAnalyticsQuery_TakeOperator_LimitsResults()
    {
        await CreateWorkspace("-take");
        var customerId = await GetCustomerId("-take");
        var records = string.Join(",", Enumerable.Range(0, 5).Select(i => $"{{\"Index\":{i}}}"));
        await IngestViaCurl(customerId, "CliTakeTable", $"[{records}]");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"CliTakeTable_CL | take 2\"",
            response =>
            {
                var rows = response.AsArray()![0]!["rows"]!.AsArray();
                Assert.That(rows!, Has.Count.EqualTo(2));
            }, 0, skipIfWarning: true);
    }

    [Test]
    public async Task LogAnalyticsQuery_WhereFilter_ReturnsOnlyMatchingRows()
    {
        await CreateWorkspace("-where");
        var customerId = await GetCustomerId("-where");
        await IngestViaCurl(customerId, "CliWhereTable",
            "[{\"Message\":\"keep-me\",\"Level\":\"Error\"},{\"Message\":\"skip-me\",\"Level\":\"Info\"}]");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"CliWhereTable_CL | where Level_s == 'Error'\"",
            response =>
            {
                var rows = response.AsArray()![0]!["rows"]!.AsArray();
                Assert.That(rows!.Count, Is.GreaterThanOrEqualTo(1));
            }, 0);
    }

    [Test]
    public async Task LogAnalyticsQuery_UnknownTable_ReturnsEmptyResult()
    {
        await CreateWorkspace("-unknown");
        var customerId = await GetCustomerId("-unknown");

        await RunAzureCliCommand(
            $"az monitor log-analytics query -w {customerId} --analytics-query \"NoSuchTable_CL | take 10\"",
            response =>
            {
                var tables = response.AsArray();
                Assert.That(tables, Is.Not.Null);
                var primaryTable = tables![0]!;
                Assert.Multiple(() =>
                {
                    Assert.That(primaryTable["name"]!.GetValue<string>(), Is.EqualTo("PrimaryResult"));
                    Assert.That(primaryTable["rows"]!.AsArray(), Is.Empty);
                });
            }, 0);
    }
}
