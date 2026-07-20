using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Topaz.Example.ApplicationInsights;

// Assumes Topaz is already running locally (e.g. via `topaz run` or Docker).
// Set the connection string from your component, e.g.:
//   InstrumentationKey=<key>;IngestionEndpoint=https://<component>.applicationinsights.topaz.local.dev:8899/
internal class Program
{
    private static readonly HttpClient Http = new();

    public static async Task Main(string[] args)
    {
        var connectionString = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
              ?? throw new InvalidOperationException(
                  "Pass the connection string as the first argument or set APPLICATIONINSIGHTS_CONNECTION_STRING.");

        Console.WriteLine("Topaz Example - Application Insights");
        Console.WriteLine($"Using connection string: {connectionString}\n");

        var config = new TelemetryConfiguration { ConnectionString = connectionString };
        var telemetry = new TelemetryClient(config);

        // ── TelemetryClient signals ──────────────────────────────────────────
        telemetry.TrackRequest("GET /api/orders",
            DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(120), "200", true);
        Console.WriteLine("Sent: TrackRequest");

        telemetry.TrackTrace("Application started", SeverityLevel.Information);
        telemetry.TrackTrace("High latency detected", SeverityLevel.Warning);
        Console.WriteLine("Sent: TrackTrace (2)");

        telemetry.TrackEvent("UserLoggedIn", new Dictionary<string, string>
        {
            ["userId"] = "user-42",
            ["method"] = "OAuth"
        });
        Console.WriteLine("Sent: TrackEvent");

        telemetry.TrackMetric("QueueDepth", 17);
        telemetry.TrackMetric("ResponseTimeMs", 84.5);
        Console.WriteLine("Sent: TrackMetric (2)");

        telemetry.TrackException(new InvalidOperationException("Example exception"));
        Console.WriteLine("Sent: TrackException");

        telemetry.TrackDependency("SQL", "SELECT * FROM Orders", "order-db",
            DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(30), true);
        telemetry.TrackDependency("HTTP", "GET /external/api", "payment-gateway",
            DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(250), false);
        Console.WriteLine("Sent: TrackDependency (2)");

        telemetry.Flush();
        Console.WriteLine("Flushed telemetry.");

        // ── ILogger integration ──────────────────────────────────────────────
        using var loggerFactory = LoggerFactory.Create(b =>
            b.AddApplicationInsights(
                cfg => cfg.ConnectionString = connectionString,
                _ => { }));

        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("ILogger -> Application Insights: information");
        logger.LogWarning("ILogger -> Application Insights: warning");
        logger.LogError(new Exception("ILogger error"), "Error via ILogger");
        Console.WriteLine("Sent: ILogger messages (3)");

        new TelemetryClient(config).Flush();

        // ── Direct HTTP /v2/track ────────────────────────────────────────────
        var ingestionEndpoint = ExtractIngestionEndpoint(connectionString).TrimEnd('/');
        var ikey = ExtractInstrumentationKey(connectionString);
        var payload = "{\"iKey\":\"" + ikey + "\",\"time\":\"" + DateTimeOffset.UtcNow.ToString("O") + "\",\"data\":{\"baseType\":\"RequestData\",\"baseData\":{\"name\":\"GET /api/http-direct\",\"duration\":\"00:00:00.050\",\"responseCode\":\"200\",\"success\":true}}}";

        using var httpContent = new StringContent(payload, System.Text.Encoding.UTF8, "application/x-json-stream");
        var response = await Http.PostAsync($"{ingestionEndpoint}/v2/track", httpContent);
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Direct /v2/track -> HTTP {(int)response.StatusCode}: {body}");

        // ── KQL query ────────────────────────────────────────────────────────
        await RunQueryAsync(ingestionEndpoint, ikey, "requests | take 10");
        await RunQueryAsync(ingestionEndpoint, ikey, "requests | summarize count() by name");
        await RunQueryAsync(ingestionEndpoint, ikey, "traces | order by timestamp desc | take 5");
        await RunQueryAsync(ingestionEndpoint, ikey, "exceptions | take 5");

        Console.WriteLine("\nDone.");
    }

    private static string ExtractInstrumentationKey(string connectionString) =>
        connectionString.Split(';')
            .First(p => p.StartsWith("InstrumentationKey=", StringComparison.OrdinalIgnoreCase))
            .Split('=', 2)[1];

    private static string ExtractIngestionEndpoint(string connectionString) =>
        connectionString.Split(';')
            .First(p => p.StartsWith("IngestionEndpoint=", StringComparison.OrdinalIgnoreCase))
            .Split('=', 2)[1];

    private static async Task RunQueryAsync(string ingestionEndpoint, string ikey, string kql)
    {
        Console.WriteLine($"\nQuery: {kql}");
        var body = "{\"query\":\"" + kql.Replace("\"", "\\\"") + "\"}";
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await Http.PostAsync($"{ingestionEndpoint}/v1/apps/{ikey}/query", content);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var table = doc.RootElement.GetProperty("tables")[0];
        var columns = table.GetProperty("columns").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString()!)
            .ToArray();
        var rows = table.GetProperty("rows").EnumerateArray().ToArray();
        Console.WriteLine(string.Join(" | ", columns));
        Console.WriteLine(new string('-', columns.Sum(c => c.Length) + (columns.Length - 1) * 3));
        foreach (var row in rows)
            Console.WriteLine(string.Join(" | ", row.EnumerateArray().Select(v => v.ToString())));
        Console.WriteLine($"({rows.Length} row(s))");
    }
}
