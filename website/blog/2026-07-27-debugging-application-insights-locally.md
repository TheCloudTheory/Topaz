---
slug: debugging-application-insights-locally
title: "Debugging Application Insights without a cloud subscription"
description: Application Insights bugs are notoriously hard to reproduce because you can't easily inspect what the SDK is actually sending. Topaz emulates the ingestion and query APIs locally, so you can trace every telemetry call, run KQL queries against the data, and fix the bug before it reaches production.
keywords: [application insights local, azure application insights emulator, application insights debug locally, topaz application insights, kql local, azure monitor local, application insights sdk debugging]
authors: kamilmrzyglod
tags: [general, testing, application-insights]
---

Application Insights bugs tend to fall into one of two categories. Either the SDK sends something and you never see it in the portal — wrong connection string, wrong environment variable, telemetry initializer stripping the data — or it sends something you did not intend, and you only find out a week later when billing shows an unexpected spike. Both categories share one root cause: there is no fast, local feedback loop.

The standard debugging advice is to [enable `DiagnosticsTelemetryModule`](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-troubleshoot-no-data#self-diagnostics), or to [tail the Fiddler capture](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-troubleshoot-no-data#fiddler), or to deploy to a dev environment and wait for the [portal to ingest the data](https://learn.microsoft.com/en-us/azure/azure-monitor/app/data-retention-privacy#how-long-is-the-data-kept). All of those paths are slow. Some require a cloud subscription you might not have access to from the machine you are debugging on.

Topaz emulates the Application Insights ingestion API (`/v2/track`) and the query API (`/v1/apps/{ikey}/query`) locally. The SDK points at localhost, sends telemetry as it normally would, and you can query the ingested data immediately — from code, from the Topaz Portal, or from a KQL query in your terminal.

{/* truncate */}

:::tip[Try it yourself]
All examples in this post work against the current stable release of Topaz.

```bash
brew tap thecloudtheory/topaz && brew install topaz   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Application Insights docs →](https://topaz.thecloudtheory.com/docs/services/application-insights) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

## A concrete scenario

Imagine an e-commerce order service. It tracks requests, logs exceptions from the payment gateway, records a `CheckoutCompleted` event, and emits a `PaymentLatencyMs` metric. At some point someone reports that the exceptions are not appearing in the portal. The request telemetry looks fine. The metric is there. But `exceptions` returns nothing.

The usual investigation path involves deploying a test build to a staging environment, triggering the path that should produce an exception, waiting several minutes for the portal to ingest, and then deciding whether the absence of data means the SDK is not sending or the portal is filtering. None of that is fast, and it only works if you have credentials for staging.

With Topaz you can reproduce and fix this in one session on a laptop.

## Setting up the local component

Start Topaz, create a resource group and a component:

```bash
topaz run

topaz group create --name my-rg --location eastus
topaz insights create \
  --name order-service \
  --resource-group my-rg \
  --location eastus
```

Get the connection string:

```bash
topaz insights component show \
  --name order-service \
  --resource-group my-rg
```

The command returns the full component JSON. Copy the `connectionString` field from the `properties` object.

The connection string looks like a real Azure one:

```
InstrumentationKey=<guid>;IngestionEndpoint=https://order-service.applicationinsights.topaz.local.dev:8899/
```

Set it as an environment variable:

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=..."
```

Your application code reads `APPLICATIONINSIGHTS_CONNECTION_STRING` the same way it does in production. No code changes required.

## What the SDK sends

With the standard `Microsoft.ApplicationInsights` NuGet package the setup is identical to production:

```csharp
var config = new TelemetryConfiguration
{
    ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
};
var telemetry = new TelemetryClient(config);

// Request telemetry
telemetry.TrackRequest(
    "POST /api/orders/checkout",
    DateTimeOffset.UtcNow,
    TimeSpan.FromMilliseconds(312),
    "200",
    success: true);

// Custom event
telemetry.TrackEvent("CheckoutCompleted", new Dictionary<string, string>
{
    ["orderId"] = "ord-8821",
    ["paymentMethod"] = "card"
});

// Metric
telemetry.TrackMetric("PaymentLatencyMs", 87.4);

// Dependency — the call to the payment gateway
telemetry.TrackDependency("HTTP", "POST /charge", "payment-gateway",
    DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(87), success: true);

// Exception — this is the one that was missing
try
{
    throw new InvalidOperationException("Payment gateway timeout");
}
catch (Exception ex)
{
    telemetry.TrackException(ex, new Dictionary<string, string>
    {
        ["orderId"] = "ord-8822",
        ["stage"] = "capture"
    });
}

telemetry.Flush();
```

The same code works against real Azure and against Topaz. The only change is the value of `APPLICATIONINSIGHTS_CONNECTION_STRING`.

## Querying the ingested data

Once the SDK has flushed, the data is queryable immediately. No ingestion delay, no portal caching.

From code, the query API accepts standard KQL:

```csharp
var ingestionEndpoint = "https://order-service.applicationinsights.topaz.local.dev:8899";
var ikey = "<guid>";

var url = $"{ingestionEndpoint}/v1/apps/{ikey}/query";
var body = new StringContent(
    """{"query": "exceptions | order by timestamp desc | take 10"}""",
    Encoding.UTF8, "application/json");

var response = await httpClient.PostAsync(url, body);
var json = await response.Content.ReadAsStringAsync();
```

The response schema is the same as the real Azure Monitor query API — `tables`, `columns`, `rows`. Code that parses real query results will parse local ones too.

From the terminal, the Topaz CLI wraps the query API:

```bash
topaz insights query \
  --name order-service \
  --resource-group my-rg \
  --query "exceptions | order by timestamp desc | take 10"
```

You get the tabular output immediately. If exceptions are missing here, the SDK is not sending them. If they are present, something downstream — a telemetry processor, a sampling configuration, a portal filter — is dropping them.

## Using the Topaz Portal

Topaz ships with a built-in web portal that exposes a Logs blade for each Application Insights component. It is the same experience as the Azure portal Logs tab, minus the ingestion delay.

Navigate to the component in the portal, open the **Logs** tab, and run any KQL query directly in the browser:

```kql
exceptions
| order by timestamp desc
| project timestamp, type, outerMessage, customDimensions
```

The results appear in a table below the editor. This is useful when you want to cross-reference telemetry types — for example, checking whether the exception timestamp aligns with the dependency call that preceded it:

```kql
union requests, dependencies, exceptions
| order by timestamp asc
| project timestamp, itemType, name, success, duration
```

No SDK, no credentials, no subscription needed. The portal talks to the same local ingestion endpoint and returns the same data.

## Finding the bug

Back to the original scenario: exceptions were missing. With Topaz you can narrow it down in a few minutes.

First, verify that the SDK is calling `/v2/track` at all. Topaz logs every ingestion request. If you see the POST but no data appears in the query results, a telemetry processor is filtering. If you do not see the POST, the SDK is not flushing — the most common cause is a missing `Flush()` call or a `TelemetryConfiguration` that is disposed before the flush completes.

Second, check sampling. The `AdaptiveSamplingTelemetryProcessor` is enabled by default when you use the full Application Insights SDK. At low request volumes in a development loop it may decide to sample out exceptions entirely. Disabling it locally confirms whether sampling is the cause:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = connectionString;

// Disable adaptive sampling for the debug session
var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
builder.UseAdaptiveSampling(maxTelemetryItemsPerSecond: 100);
builder.Build();
```

After the fix, run the query again. If exceptions appear now, you have the root cause without deploying anything.

## ILogger integration

Most .NET services do not call `TelemetryClient` directly. They use `ILogger`, and the Application Insights sink forwards log entries as traces or exceptions depending on the log level. That path goes through the same ingestion endpoint and is equally inspectable with Topaz:

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddApplicationInsights(
        cfg => cfg.ConnectionString = connectionString,
        _ => { }));

var logger = loggerFactory.CreateLogger<OrderService>();

logger.LogInformation("Order {OrderId} received", "ord-8821");
logger.LogWarning("Payment latency above threshold: {Ms}ms", 340);
logger.LogError(new InvalidOperationException("Capture failed"), "Payment capture failed for order {OrderId}", "ord-8822");
```

Query the result:

```kql
traces
| where severityLevel >= 2
| order by timestamp desc
| project timestamp, severityLevel, message
```

The `severityLevel` mapping follows the standard Application Insights convention: 0 Verbose, 1 Information, 2 Warning, 3 Error, 4 Critical.

## Summary

Topaz gives you a complete local Application Insights ingestion and query stack. The SDK configuration is identical to production — only the connection string endpoint changes. You can flush telemetry, query it immediately with KQL from code or from the terminal, and inspect it in the Portal Logs blade without waiting for cloud ingestion or holding a subscription.

The feedback loop for "is this SDK call actually reaching the backend and storing the right data" goes from several minutes (deploy, trigger, wait, check portal) to a few seconds. That is the difference between debugging Application Insights problems and shipping them.

[Application Insights docs →](https://topaz.thecloudtheory.com/docs/services/application-insights) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
