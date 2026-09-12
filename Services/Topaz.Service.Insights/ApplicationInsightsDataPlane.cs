using System.Text.Json.Nodes;
using Topaz.EventPipeline;
using Topaz.Service.Insights.Kql;
using Topaz.Service.Insights.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class ApplicationInsightsDataPlane(
    ApplicationInsightsResourceProvider provider,
    ApplicationInsightsServiceControlPlane controlPlane,
    ITopazLogger logger)
{
    private static readonly IReadOnlyDictionary<string, string> BaseTypeToTable =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RequestData"] = "requests",
            ["TraceData"] = "traces",
            ["ExceptionData"] = "exceptions",
            ["EventData"] = "customEvents",
            ["MetricData"] = "customMetrics",
            ["RemoteDependencyData"] = "dependencies",
        };

    public static ApplicationInsightsDataPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new ApplicationInsightsResourceProvider(logger),
        ApplicationInsightsServiceControlPlane.New(eventPipeline, logger),
        logger);

    public DataPlaneOperationResult<IngestionEnvelope> Ingest(string instrumentationKey, string type, string content)
    {
        var componentResult = controlPlane.GetByInstrumentationKey(instrumentationKey);
        if (componentResult.Result != OperationResult.Success || componentResult.Resource == null)
        {
            return new DataPlaneOperationResult<IngestionEnvelope>(OperationResult.NotFound,
                null, "Component not found", "ComponentNotFound");
        }

        var component = componentResult.Resource;
        var sub = component.GetSubscription();
        var rg = component.GetResourceGroup();

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var accepted = 0;

        foreach (var line in lines)
        {
            try
            {
                var node = JsonNode.Parse(line);

                var baseType = node?["data"]?["baseType"]?.GetValue<string>();
                if (baseType == null || !BaseTypeToTable.TryGetValue(baseType, out var tableName))
                {
                    continue;
                }

                // Flatten: promote data.baseData fields to top level for query ease
                var envelope = new JsonObject
                {
                    ["timestamp"] = node!["time"]?.GetValue<string>() ?? DateTimeOffset.UtcNow.ToString("O"),
                    ["iKey"] = node["iKey"]?.GetValue<string>()
                };

                var baseData = node["data"]?["baseData"];
                if (baseData is JsonObject bd)
                {
                    foreach (var prop in bd)
                    {
                        envelope[prop.Key] = prop.Value?.DeepClone();
                    }
                }

                provider.SaveTelemetry(sub, rg, component.Name, tableName, envelope.ToJsonString());
                accepted++;
            }
            catch (Exception ex)
            {
                logger.LogError(nameof(ApplicationInsightsDataPlane), nameof(Ingest),
                    "Failed to persist envelope line: {0}", ex.Message);
            }
        }

        return new DataPlaneOperationResult<IngestionEnvelope>(OperationResult.Success,
            new IngestionEnvelope { ItemsReceived = lines.Length, ItemsAccepted = accepted });
    }

    public DataPlaneOperationResult<QueryResult> Query(string instrumentationKey, string queryText)
    {
        var componentResult = controlPlane.GetByInstrumentationKey(instrumentationKey);
        if (componentResult.Result != OperationResult.Success || componentResult.Resource == null)
        {
            return new DataPlaneOperationResult<QueryResult>(OperationResult.NotFound,
                null, "Component not found", "ComponentNotFound");
        }

        try
        {
            var component = componentResult.Resource;
            var result = KqlQueryExecutor.Execute(
                queryText,
                tableName => provider.LoadTelemetry(
                    component.GetSubscription(), component.GetResourceGroup(), component.Name, tableName));

            return new DataPlaneOperationResult<QueryResult>(OperationResult.Success, result);
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(ApplicationInsightsDataPlane), nameof(Query),
                $"Error during performing Application Insights query: {ex.Message}. {ex.StackTrace}");
            
            return new DataPlaneOperationResult<QueryResult>(OperationResult.Failed, null);
        }
    }
}