using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Topaz.EventPipeline;
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
            ["DependencyData"] = "dependencies",
        };

    public static ApplicationInsightsDataPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new ApplicationInsightsResourceProvider(logger),
        ApplicationInsightsServiceControlPlane.New(eventPipeline, logger),
        logger);

    public DataPlaneOperationResult<IngestionEnvelope> Ingest(string instrumentationKey, string type, string content)
    {
        var componentResult = controlPlane.GetByInstrumentationKey(instrumentationKey);
        if (componentResult.Result != OperationResult.Success || componentResult.Resource == null)
            return new DataPlaneOperationResult<IngestionEnvelope>(OperationResult.NotFound,
                null, "Component not found", "ComponentNotFound");

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
                if (node == null) continue;

                var baseType = node["data"]?["baseType"]?.GetValue<string>();
                if (baseType == null || !BaseTypeToTable.TryGetValue(baseType, out var tableName))
                    continue;

                // Flatten: promote data.baseData fields to top level for query ease
                var envelope = new JsonObject();
                envelope["timestamp"] = node["time"]?.GetValue<string>() ?? DateTimeOffset.UtcNow.ToString("O");
                envelope["iKey"] = node["iKey"]?.GetValue<string>();

                var baseData = node["data"]?["baseData"];
                if (baseData is JsonObject bd)
                {
                    foreach (var prop in bd)
                        envelope[prop.Key] = prop.Value?.DeepClone();
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
            new IngestionEnvelope { ItemsReceived = lines.Length, ItemsAccepted = accepted }, null, null);
    }

    public DataPlaneOperationResult<QueryResult> Query(string instrumentationKey, string queryText)
    {
        var componentResult = controlPlane.GetByInstrumentationKey(instrumentationKey);
        if (componentResult.Result != OperationResult.Success || componentResult.Resource == null)
            return new DataPlaneOperationResult<QueryResult>(OperationResult.NotFound,
                null, "Component not found", "ComponentNotFound");

        var component = componentResult.Resource;
        var result = KqlQueryExecutor.Execute(
            queryText,
            tableName => provider.LoadTelemetry(
                component.GetSubscription(), component.GetResourceGroup(), component.Name, tableName));

        return new DataPlaneOperationResult<QueryResult>(OperationResult.Success, result, null, null);
    }

    private static class KqlQueryExecutor
    {
        public static QueryResult Execute(string queryText, Func<string, IEnumerable<string>> tableLoader)
        {
            queryText = queryText.Trim();
            var pipes = queryText.Split('|');

            var tableName = pipes[0].Trim();
            var rows = tableLoader(tableName)
                .Select(json =>
                {
                    try { return JsonNode.Parse(json) as JsonObject; }
                    catch { return null; }
                })
                .OfType<JsonObject>()
                .ToList();

            var operators = pipes.Skip(1).Select(p => p.Trim()).ToList();

            foreach (var op in operators)
            {
                if (op.StartsWith("where ", StringComparison.OrdinalIgnoreCase))
                    rows = ApplyWhere(rows, op["where ".Length..].Trim());
                else if (op.StartsWith("project ", StringComparison.OrdinalIgnoreCase))
                    rows = ApplyProject(rows, op["project ".Length..].Trim());
                else if (op.StartsWith("summarize ", StringComparison.OrdinalIgnoreCase))
                    rows = ApplySummarize(rows, op["summarize ".Length..].Trim());
                else if (op.StartsWith("order by ", StringComparison.OrdinalIgnoreCase))
                    rows = ApplyOrderBy(rows, op["order by ".Length..].Trim());
                else if (op.StartsWith("take ", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(op["take ".Length..].Trim(), out var n))
                        rows = rows.Take(n).ToList();
                }
            }

            // Derive columns from union of all field names
            var columns = rows.SelectMany(r => r.Select(kvp => kvp.Key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new QueryColumn(name, "string"))
                .ToArray();

            var resultRows = rows.Select(r =>
                columns.Select(c => (object?)(r[c.Name]?.GetValue<object>())).ToArray()
            ).ToArray();

            return new QueryResult([new QueryResultTable("PrimaryResult", columns, resultRows)]);
        }

        private static List<JsonObject> ApplyWhere(List<JsonObject> rows, string predicate)
        {
            // where <field> == "<value>"
            var eqMatch = Regex.Match(predicate, @"^(\w+)\s*==\s*""([^""]*)""$");
            if (eqMatch.Success)
            {
                var field = eqMatch.Groups[1].Value;
                var value = eqMatch.Groups[2].Value;
                return rows.Where(r => string.Equals(r[field]?.GetValue<string>(), value, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // where <field> contains "<value>"
            var containsMatch = Regex.Match(predicate, @"^(\w+)\s+contains\s+""([^""]*)""$", RegexOptions.IgnoreCase);
            if (containsMatch.Success)
            {
                var field = containsMatch.Groups[1].Value;
                var value = containsMatch.Groups[2].Value;
                return rows.Where(r => r[field]?.GetValue<string>()?.Contains(value, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // where <field> startswith "<value>"
            var startsMatch = Regex.Match(predicate, @"^(\w+)\s+startswith\s+""([^""]*)""$", RegexOptions.IgnoreCase);
            if (startsMatch.Success)
            {
                var field = startsMatch.Groups[1].Value;
                var value = startsMatch.Groups[2].Value;
                return rows.Where(r => r[field]?.GetValue<string>()?.StartsWith(value, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            return rows;
        }

        private static List<JsonObject> ApplyProject(List<JsonObject> rows, string columns)
        {
            var cols = columns.Split(',').Select(c => c.Trim()).ToArray();
            return rows.Select(r =>
            {
                var obj = new JsonObject();
                foreach (var c in cols)
                    obj[c] = r[c]?.DeepClone();
                return obj;
            }).ToList();
        }

        private static List<JsonObject> ApplySummarize(List<JsonObject> rows, string expression)
        {
            // summarize count() [by <field>]
            var byMatch = Regex.Match(expression, @"^count\(\)\s+by\s+(\w+)$", RegexOptions.IgnoreCase);
            if (byMatch.Success)
            {
                var groupField = byMatch.Groups[1].Value;
                return rows
                    .GroupBy(r => r[groupField]?.GetValue<string>() ?? "")
                    .Select(g =>
                    {
                        var obj = new JsonObject();
                        obj[groupField] = JsonValue.Create(g.Key);
                        obj["count_"] = JsonValue.Create(g.Count());
                        return obj;
                    }).ToList();
            }

            // summarize count()
            if (Regex.IsMatch(expression, @"^count\(\)$", RegexOptions.IgnoreCase))
            {
                var obj = new JsonObject();
                obj["count_"] = JsonValue.Create(rows.Count);
                return [obj];
            }

            return rows;
        }

        private static List<JsonObject> ApplyOrderBy(List<JsonObject> rows, string expression)
        {
            // order by <field> [asc|desc]
            var match = Regex.Match(expression, @"^(\w+)\s*(asc|desc)?$", RegexOptions.IgnoreCase);
            if (!match.Success) return rows;

            var field = match.Groups[1].Value;
            var desc = !match.Groups[2].Success || match.Groups[2].Value.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return desc
                ? rows.OrderByDescending(r => r[field]?.GetValue<string>()).ToList()
                : rows.OrderBy(r => r[field]?.GetValue<string>()).ToList();
        }
    }
}