using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Topaz.Service.LogAnalytics;

internal static class KqlQueryExecutor
{
    private static readonly HashSet<string> BuiltInEmptyTables =
        new(StringComparer.OrdinalIgnoreCase) { "AzureActivity", "AzureDiagnostics" };

    public static LogAnalyticsQueryResult Execute(
        string queryText,
        Func<string, IEnumerable<string>> tableLoader)
    {
        queryText = queryText.Trim();
        var pipes = queryText.Split('|');

        var tableExpr = pipes[0].Trim();

        // union TableA, TableB or union (TableA | ...), (TableB | ...)
        List<JsonObject> rows;
        if (tableExpr.StartsWith("union ", StringComparison.OrdinalIgnoreCase))
        {
            var tableNames = tableExpr["union ".Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());
            rows = tableNames.SelectMany(t => LoadTable(t, tableLoader)).ToList();
        }
        else
        {
            rows = LoadTable(tableExpr, tableLoader).ToList();
        }

        foreach (var op in pipes.Skip(1).Select(p => p.Trim()))
        {
            if (op.StartsWith("where ", StringComparison.OrdinalIgnoreCase))
                rows = ApplyWhere(rows, op["where ".Length..].Trim());
            else if (op.StartsWith("project ", StringComparison.OrdinalIgnoreCase))
                rows = ApplyProject(rows, op["project ".Length..].Trim());
            else if (op.StartsWith("extend ", StringComparison.OrdinalIgnoreCase))
                rows = ApplyExtend(rows, op["extend ".Length..].Trim());
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

        var columns = rows.SelectMany(r => r.Select(kvp => kvp.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(n => new LogAnalyticsQueryColumn(n, "string"))
            .ToArray();

        var resultRows = rows.Select(r =>
            columns.Select(c => r[c.Name]?.GetValue<object>()).ToArray()
        ).ToArray();

        return new LogAnalyticsQueryResult(
            [new LogAnalyticsQueryTable("PrimaryResult", columns, resultRows)]);
    }

    private static List<JsonObject> LoadTable(
        string tableName,
        Func<string, IEnumerable<string>> tableLoader)
    {
        if (BuiltInEmptyTables.Contains(tableName))
            return [];

        return tableLoader(tableName)
            .SelectMany(json =>
            {
                try
                {
                    var node = JsonNode.Parse(json);
                    // Stored as JSON array (batch ingestion) or single object
                    return node switch
                    {
                        JsonArray arr => arr.OfType<JsonObject>(),
                        JsonObject obj => (IEnumerable<JsonObject>)[obj],
                        _ => []
                    };
                }
                catch (Exception)
                {
                    return [];
                }
            })
            .ToList();
    }

    private static List<JsonObject> ApplyWhere(List<JsonObject> rows, string predicate)
    {
        // field == "value"
        var eqStr = Regex.Match(predicate, @"^(\w+)\s*==\s*""([^""]*)""");
        if (eqStr.Success)
        {
            var f = eqStr.Groups[1].Value;
            var v = eqStr.Groups[2].Value;
            return rows.Where(r => string.Equals(r[f]?.GetValue<string>(), v, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // field == number
        var eqNum = Regex.Match(predicate, @"^(\w+)\s*==\s*([\d.]+)$");
        if (eqNum.Success && double.TryParse(eqNum.Groups[2].Value, out var numVal))
        {
            var f = eqNum.Groups[1].Value;
            return rows.Where(r => r[f] is JsonValue jv && jv.TryGetValue<double>(out var d) && d == numVal).ToList();
        }

        // field contains "value"
        var contains = Regex.Match(predicate, @"^(\w+)\s+contains\s+""([^""]*)""", RegexOptions.IgnoreCase);
        if (contains.Success)
        {
            var f = contains.Groups[1].Value;
            var v = contains.Groups[2].Value;
            return rows.Where(r => r[f]?.GetValue<string>()?.Contains(v, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        // field startswith "value"
        var starts = Regex.Match(predicate, @"^(\w+)\s+startswith\s+""([^""]*)""", RegexOptions.IgnoreCase);
        if (starts.Success)
        {
            var f = starts.Groups[1].Value;
            var v = starts.Groups[2].Value;
            return rows.Where(r => r[f]?.GetValue<string>()?.StartsWith(v, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        // field between (low .. high)  — numeric
        var between = Regex.Match(predicate, @"^(\w+)\s+between\s+\(([\d.]+)\s*\.\.\s*([\d.]+)\)",
            RegexOptions.IgnoreCase);
        if (between.Success
            && double.TryParse(between.Groups[2].Value, out var lo)
            && double.TryParse(between.Groups[3].Value, out var hi))
        {
            var f = between.Groups[1].Value;
            return rows.Where(r =>
                r[f] is JsonValue jv && jv.TryGetValue<double>(out var d) && d >= lo && d <= hi).ToList();
        }

        return rows;
    }

    private static List<JsonObject> ApplyProject(List<JsonObject> rows, string columns)
    {
        var cols = columns.Split(',').Select(c => c.Trim()).ToArray();
        return rows.Select(r =>
        {
            var obj = new JsonObject();
            foreach (var c in cols) obj[c] = r[c]?.DeepClone();
            return obj;
        }).ToList();
    }

    private static List<JsonObject> ApplyExtend(List<JsonObject> rows, string expression)
    {
        // alias = field  or  alias = 'literal'
        var m = Regex.Match(expression, @"^(\w+)\s*=\s*(.+)$");
        if (!m.Success) return rows;
        var alias = m.Groups[1].Value;
        var valueExpr = m.Groups[2].Value.Trim();

        return rows.Select(r =>
        {
            var clone = JsonNode.Parse(r.ToJsonString())!.AsObject();
            if (valueExpr.StartsWith('"') && valueExpr.EndsWith('"'))
                clone[alias] = JsonValue.Create(valueExpr[1..^1]);
            else if (r[valueExpr] != null)
                clone[alias] = r[valueExpr]?.DeepClone();
            else
                clone[alias] = JsonValue.Create(valueExpr);
            return clone;
        }).ToList();
    }

    private static List<JsonObject> ApplySummarize(List<JsonObject> rows, string expression)
    {
        // summarize <agg>(<field>) [by <groupField>]
        // agg: count, sum, avg, min, max
        var m = Regex.Match(expression,
            @"^(count|sum|avg|min|max)\((\w*)\)(?:\s+by\s+(\w+))?$",
            RegexOptions.IgnoreCase);
        if (!m.Success) return rows;

        var aggFn = m.Groups[1].Value.ToLowerInvariant();
        var aggField = m.Groups[2].Value;
        var groupField = m.Groups[3].Success ? m.Groups[3].Value : null;

        var outputField = aggFn == "count" ? "count_" : $"{aggFn}_{aggField}";

        var groups = groupField != null
            ? rows.GroupBy(r => r[groupField]?.GetValue<string>() ?? "")
            : rows.GroupBy(_ => "");

        return groups.Select(g =>
        {
            var obj = new JsonObject();
            if (groupField != null) obj[groupField] = JsonValue.Create(g.Key);
            obj[outputField] = aggFn switch
            {
                "count" => JsonValue.Create(g.Count()),
                "sum" => JsonValue.Create(g.Sum(r => GetDouble(r, aggField))),
                "avg" => JsonValue.Create(g.Average(r => GetDouble(r, aggField))),
                "min" => JsonValue.Create(g.Min(r => GetDouble(r, aggField))),
                "max" => JsonValue.Create(g.Max(r => GetDouble(r, aggField))),
                _ => JsonValue.Create(0)
            };
            return obj;
        }).ToList();
    }

    private static List<JsonObject> ApplyOrderBy(List<JsonObject> rows, string expression)
    {
        var m = Regex.Match(expression, @"^(\w+)\s*(asc|desc)?$", RegexOptions.IgnoreCase);
        if (!m.Success) return rows;
        var field = m.Groups[1].Value;
        var desc = !m.Groups[2].Success || m.Groups[2].Value.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return desc
            ? rows.OrderByDescending(r => r[field]?.GetValue<string>()).ToList()
            : rows.OrderBy(r => r[field]?.GetValue<string>()).ToList();
    }

    private static double GetDouble(JsonObject r, string field)
    {
        if (r[field] is JsonValue jv && jv.TryGetValue<double>(out var d)) return d;
        return 0;
    }
}

internal sealed class LogAnalyticsQueryResult(LogAnalyticsQueryTable[] tables)
{
    public LogAnalyticsQueryTable[] Tables { get; } = tables;
}

internal sealed class LogAnalyticsQueryTable(string name, LogAnalyticsQueryColumn[] columns, object?[][] rows)
{
    public string Name { get; } = name;
    public LogAnalyticsQueryColumn[] Columns { get; } = columns;
    public object?[][] Rows { get; } = rows;
}

internal sealed class LogAnalyticsQueryColumn(string name, string type)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
}