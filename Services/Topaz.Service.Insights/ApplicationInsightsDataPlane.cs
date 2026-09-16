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
                component.Name,
                (workspaceName, tableName) => provider.LoadTelemetry(
                    component.GetSubscription(), component.GetResourceGroup(), workspaceName, tableName),
                ResolveWorkspaceName,
                (appReference, tableName) => LoadAppTelemetry(component, appReference, tableName));

            return new DataPlaneOperationResult<QueryResult>(OperationResult.Success, result);
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(ApplicationInsightsDataPlane), nameof(Query),
                $"Error during performing Application Insights query: {ex.Message}. {ex.StackTrace}");
            
            return new DataPlaneOperationResult<QueryResult>(OperationResult.Failed, null);
        }
    }
    
    private IEnumerable<string> LoadAppTelemetry(
        ApplicationInsightsComponentResource currentComponent, string appReference, string tableName)
    {
        // app() takes either a component name or its fully-qualified resource ID; the component may
        // live in another resource group or subscription, so it is resolved before loading telemetry.
        var appName = appReference.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(appName))
        {
            return [];
        }

        var target = string.Equals(appName, currentComponent.Name, StringComparison.OrdinalIgnoreCase)
            ? currentComponent
            : ResolveComponentByName(appName);

        return target == null
            ? []
            : provider.LoadTelemetry(target.GetSubscription(), target.GetResourceGroup(), target.Name, tableName);
    }

    private ApplicationInsightsComponentResource? ResolveComponentByName(string componentName)
    {
        var result = controlPlane.GetByName(componentName);
        return result.Result == OperationResult.Success ? result.Resource : null;
    }

    private string ResolveWorkspaceName(string workspaceRef)
    {
        return workspaceRef;

    }
}