using System.Text.Json;
using System.Text.Json.Nodes;

namespace Topaz.Service.CosmosDb.SqlQuery;

/// <summary>
/// Executes a <see cref="ParsedQuery"/> against an in-memory document set and
/// returns the projected result page together with the next-page skip offset
/// for <c>x-ms-continuation</c> pagination.
/// </summary>
internal sealed class CosmosDbSqlExecutor
{
    /// <summary>
    /// Navigates a dot-separated property path (e.g. <c>address.city</c>) within
    /// a document. Returns <c>null</c> when any segment is missing.
    /// </summary>
    internal static JsonNode? GetProperty(JsonObject doc, string path)
    {
        if (string.IsNullOrEmpty(path)) return doc;

        JsonNode? current = doc;
        foreach (var part in path.Split('.'))
        {
            if (current is not JsonObject obj) return null;
            current = obj[part];
        }

        return current;
    }

    /// <summary>
    /// Compares a document <see cref="JsonNode"/> against a resolved literal value
    /// using the given comparison operator. Type-aware: numeric, string, and boolean
    /// comparisons are all supported.
    /// </summary>
    internal static bool Compare(JsonNode? left, ComparisonOp op, object? right)
    {
        // Null comparison
        if (right == null)
        {
            var isNull = left == null ||
                         (left is JsonValue jv && jv.GetValueKind() == JsonValueKind.Null);
            return op switch
            {
                ComparisonOp.Eq  => isNull,
                ComparisonOp.Neq => !isNull,
                _                => false
            };
        }

        if (left == null || (left is JsonValue nullJv && nullJv.GetValueKind() == JsonValueKind.Null))
            return op == ComparisonOp.Neq;

        // Numeric
        if (right is double rd && left is JsonValue numJv && numJv.TryGetValue<double>(out var ld))
        {
            return op switch
            {
                ComparisonOp.Eq  => ld == rd,
                ComparisonOp.Neq => ld != rd,
                ComparisonOp.Lt  => ld <  rd,
                ComparisonOp.Lte => ld <= rd,
                ComparisonOp.Gt  => ld >  rd,
                ComparisonOp.Gte => ld >= rd,
                _                => false
            };
        }

        // String
        if (right is string rs && left is JsonValue strJv && strJv.TryGetValue<string>(out var ls))
        {
            var cmp = string.Compare(ls, rs, StringComparison.Ordinal);
            return op switch
            {
                ComparisonOp.Eq  => cmp == 0,
                ComparisonOp.Neq => cmp != 0,
                ComparisonOp.Lt  => cmp <  0,
                ComparisonOp.Lte => cmp <= 0,
                ComparisonOp.Gt  => cmp >  0,
                ComparisonOp.Gte => cmp >= 0,
                _                => false
            };
        }

        // Boolean
        if (right is bool rb && left is JsonValue boolJv && boolJv.TryGetValue<bool>(out var lb))
        {
            return op switch
            {
                ComparisonOp.Eq  => lb == rb,
                ComparisonOp.Neq => lb != rb,
                _                => false
            };
        }

        return false;
    }

    /// <summary>
    /// Executes the parsed query against the provided documents.
    /// </summary>
    /// <param name="docs">All documents in the collection.</param>
    /// <param name="query">Parsed SQL query.</param>
    /// <param name="parameters">Resolved parameter map (@name → value).</param>
    /// <param name="skip">Number of results to skip (HTTP continuation offset).</param>
    /// <param name="maxCount">Maximum number of results to return in this page.</param>
    internal ExecutionResult Execute(
        IEnumerable<JsonObject> docs,
        ParsedQuery query,
        IReadOnlyDictionary<string, JsonNode?> parameters,
        int skip,
        int maxCount)
    {
        // 1. Filter
        IEnumerable<JsonObject> filtered = query.Where == null
            ? docs
            : docs.Where(d => query.Where.Evaluate(d, parameters));

        // 2. GROUP BY — partition then aggregate per group
        if (query.GroupByField != null)
        {
            var groupField = query.GroupByField;
            var groups = filtered
                .GroupBy(d => GetProperty(d, groupField)?.ToJsonString() ?? "null")
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToArray();

            var groupResults = new List<JsonNode>();
            foreach (var group in groups)
            {
                var groupDocs = group.ToArray();
                var row = new JsonObject();

                // Inject the GROUP BY field value
                var groupKeyNode = GetProperty(groupDocs[0], groupField);
                var fieldName = groupField.Contains('.')
                    ? groupField[(groupField.LastIndexOf('.') + 1)..]
                    : groupField;
                row[fieldName] = groupKeyNode?.DeepClone();

                // Compute each aggregate
                var idx = 1;
                foreach (var item in query.Select.Items)
                {
                    if (item is not AggregateSelectItem agg) continue;
                    var key = agg.Alias ?? $"${idx++}";
                    row[key] = JsonValue.Create(ComputeAggregate(groupDocs, agg));
                }

                groupResults.Add(row);
            }

            return new ExecutionResult([.. groupResults], null);
        }

        // 3. Aggregate mode — compute and return immediately (no pagination)
        if (query.Select.Items.OfType<AggregateSelectItem>().Any())
        {
            var all = filtered.ToArray();
            var results = ComputeAggregates(all, query.Select);
            return new ExecutionResult(results, null);
        }

        // 4. Sort
        if (query.OrderByPath != null)
        {
            var path = query.OrderByPath;
            filtered = query.OrderByAscending
                ? filtered.OrderBy(d => GetComparableValue(GetProperty(d, path)), NullFirstComparer.Instance)
                : filtered.OrderByDescending(d => GetComparableValue(GetProperty(d, path)), NullFirstComparer.Instance);
        }

        // 5. SQL-level OFFSET/LIMIT takes precedence over HTTP pagination
        if (query.Offset > 0 || query.Limit.HasValue)
        {
            filtered = filtered.Skip(query.Offset);
            if (query.Limit.HasValue) filtered = filtered.Take(query.Limit.Value);
            var limitedResults = filtered
                .Select(d => Project(d, query.Select))
                .ToArray();
            return new ExecutionResult(limitedResults, null);
        }

        // 6. HTTP continuation-token pagination
        var filteredArray = filtered.ToArray();
        var page = filteredArray.Skip(skip).Take(maxCount).ToArray();
        int? nextSkip = (skip + page.Length < filteredArray.Length) ? skip + page.Length : null;

        // 7. Project
        var projected = page.Select(d => Project(d, query.Select)).ToArray();
        return new ExecutionResult(projected, nextSkip);
    }

    private static JsonNode[] ComputeAggregates(JsonObject[] docs, SelectClause select)
    {
        if (select.Items.Length == 1 && select.Items[0] is AggregateSelectItem single)
        {
            var value = ComputeAggregate(docs, single);
            if (select.IsValue)
                return [JsonValue.Create(value)];

            var key = single.Alias ?? "$1";
            return [new JsonObject { [key] = JsonValue.Create(value) }];
        }

        // Multiple aggregate items
        var result = new JsonObject();
        var idx = 1;
        foreach (var item in select.Items)
        {
            if (item is not AggregateSelectItem agg) continue;
            var key = agg.Alias ?? $"${idx++}";
            result[key] = JsonValue.Create(ComputeAggregate(docs, agg));
        }

        return [result];
    }

    private static double ComputeAggregate(JsonObject[] docs, AggregateSelectItem agg) =>
        agg.Function switch
        {
            AggregateFunction.Count => docs.Length,
            AggregateFunction.Sum   => docs.Sum(d => GetNumericValue(GetProperty(d, agg.PropertyPath ?? "")) ?? 0),
            AggregateFunction.Min   => docs.Length == 0 ? 0 : docs.Min(d => GetNumericValue(GetProperty(d, agg.PropertyPath ?? "")) ?? 0),
            AggregateFunction.Max   => docs.Length == 0 ? 0 : docs.Max(d => GetNumericValue(GetProperty(d, agg.PropertyPath ?? "")) ?? 0),
            AggregateFunction.Avg   => docs.Length == 0 ? 0 : docs.Average(d => GetNumericValue(GetProperty(d, agg.PropertyPath ?? "")) ?? 0),
            _                       => 0
        };

    private static double? GetNumericValue(JsonNode? node) =>
        node is JsonValue jv && jv.TryGetValue<double>(out var d) ? d : null;

    private static JsonNode Project(JsonObject doc, SelectClause select)
    {
        // Wildcard — return the full document
        if (select.IsWildcard) return doc.DeepClone();

        // VALUE property — return the raw field value
        if (select.IsValue && select.Items.Length == 1 && select.Items[0] is PropertySelectItem pi)
        {
            var val = GetProperty(doc, pi.PropertyPath);
            return val?.DeepClone() ?? JsonValue.Create((string?)null)!;
        }

        // Field projection — build a new object with only the requested fields
        var projected = new JsonObject();
        foreach (var item in select.Items)
        {
            if (item is not PropertySelectItem p) continue;
            var fieldName = p.Alias
                ?? (p.PropertyPath.Contains('.')
                    ? p.PropertyPath[(p.PropertyPath.LastIndexOf('.') + 1)..]
                    : p.PropertyPath);
            projected[fieldName] = GetProperty(doc, p.PropertyPath)?.DeepClone();
        }

        return projected;
    }

    private static IComparable? GetComparableValue(JsonNode? node)
    {
        if (node is not JsonValue jv) return null;
        if (jv.TryGetValue<double>(out var d)) return d;
        if (jv.TryGetValue<string>(out var s)) return s;
        if (jv.TryGetValue<bool>(out var b)) return b ? 1 : 0;
        return null;
    }

    private sealed class NullFirstComparer : IComparer<IComparable?>
    {
        internal static readonly NullFirstComparer Instance = new();

        public int Compare(IComparable? x, IComparable? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.CompareTo(y);
        }
    }
}

internal sealed record ExecutionResult(JsonNode[] Results, int? NextSkip);
