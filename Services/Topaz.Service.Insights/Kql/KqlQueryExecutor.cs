using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Topaz.Service.Insights.Models;

namespace Topaz.Service.Insights.Kql;

internal static partial class KqlQueryExecutor
{
    public static QueryResult Execute(string queryText, Func<string, IEnumerable<string>> tableLoader)
    {
        queryText = queryText.Trim();
        var pipes = queryText.Split('|');

        var tableName = pipes[0].Trim();
        var rows = tableLoader(tableName)
            .Select(json =>
            {
                try
                {
                    return JsonNode.Parse(json) as JsonObject;
                }
                catch
                {
                    return null;
                }
            })
            .OfType<JsonObject>()
            .ToList();

        var operators = pipes.Skip(1).Select(p => p.Trim()).ToList();

        foreach (var op in operators)
        {
            var compiled = CompileFunctions(op);

            if (compiled.StartsWith("where ", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplyWhere(rows, compiled["where ".Length..].Trim());
            }
            else if (compiled.StartsWith("project ", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplyProject(rows, compiled["project ".Length..].Trim());
            }
            else if (compiled.StartsWith("summarize ", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplySummarize(rows, compiled["summarize ".Length..].Trim());
            }
            else if (compiled.StartsWith("order by ", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplyOrderBy(rows, compiled["order by ".Length..].Trim());
            }
            else if (compiled.StartsWith("take ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(compiled["take ".Length..].Trim(), out var n))
                    rows = [.. rows.Take(n)];
            }
            else if(compiled.StartsWith("join ", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplyJoin(rows, compiled["join ".Length..].Trim(), tableLoader);
            }
        }

        // Derive columns from union of all field names
        var columns = rows.SelectMany(r => r.Select(kvp => kvp.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new QueryColumn(name, "string"))
            .ToArray();

        var resultRows = rows.Select(r =>
            columns.Select(c => r[c.Name]?.GetValue<object>()).ToArray()
        ).ToArray();

        return new QueryResult([new QueryResultTable("PrimaryResult", columns, resultRows)]);
    }

    private static string CompileFunctions(string op)
    {
        var agoMatch = AgoRegex().Match(op);
        if (agoMatch.Success)
        {
            op = AgoRegex().Replace(op, match =>
            {
                var timespan = ParseKqlTimeSpan(match.Groups[1].Value);
                var resolved = DateTimeOffset.UtcNow - timespan;
                return $"\"{resolved:O}\"";
            });
        }
        
        return op;
    }

    private static List<JsonObject> ApplyWhere(List<JsonObject> rows, string predicate)
    {
        // where <field> == "<value>"
        var eqMatch = EqualsRegex().Match(predicate);
        if (eqMatch.Success)
        {
            var field = eqMatch.Groups[1].Value;
            var value = eqMatch.Groups[2].Value;
            return
            [
                .. rows.Where(r =>
                    string.Equals(r[field]?.GetValue<string>(), value, StringComparison.OrdinalIgnoreCase))
            ];
        }

        // where <field> < "<value>"
        var ltMatch = LessThanRegex().Match(predicate);
        if (ltMatch.Success)
        {
            var field = ltMatch.Groups[1].Value;
            var value = ltMatch.Groups[2].Value;
            return
            [
                .. rows.Where(r =>
                    r[field]!.GetValue<object>().AsComparableObject().IsLessThan(value))
            ];
        }

        // where <field> > "<value>"
        var gtMatch = GreaterThanRegex().Match(predicate);
        if (gtMatch.Success)
        {
            var field = gtMatch.Groups[1].Value;
            var value = gtMatch.Groups[2].Value;
            return
            [
                .. rows.Where(r =>
                    r[field]!.GetValue<object>().AsComparableObject().IsGreaterThan(value))
            ];
        }

        // where <field> contains "<value>"
        var containsMatch = ContainsRegex().Match(predicate);
        if (containsMatch.Success)
        {
            var field = containsMatch.Groups[1].Value;
            var value = containsMatch.Groups[2].Value;
            return
            [
                .. rows.Where(r =>
                    r[field]?.GetValue<string>().Contains(value, StringComparison.OrdinalIgnoreCase) == true)
            ];
        }

        // where <field> startswith "<value>"
        var startsMatch = StartsWithRegex().Match(predicate);
        if (startsMatch.Success)
        {
            var field = startsMatch.Groups[1].Value;
            var value = startsMatch.Groups[2].Value;
            return
            [
                .. rows.Where(r =>
                    r[field]?.GetValue<string>().StartsWith(value, StringComparison.OrdinalIgnoreCase) == true)
            ];
        }

        return rows;
    }

    private static List<JsonObject> ApplyProject(List<JsonObject> rows, string columns)
    {
        var cols = columns.Split(',').Select(c => c.Trim()).ToArray();
        return
        [
            .. rows.Select(r =>
            {
                var obj = new JsonObject();
                foreach (var c in cols)
                    obj[c] = r[c]?.DeepClone();
                return obj;
            })
        ];
    }
    
    private static List<JsonObject> ApplyJoin(List<JsonObject> rows, string columns, Func<string, IEnumerable<string>> tableLoader)
    {
        var defaultJoinMatch = DefaultJoinRegex().Match(columns);
        if (defaultJoinMatch.Success)
        {
            var joinedTableRows = tableLoader(defaultJoinMatch.Groups[1].Value).Select(json =>
                {
                    try
                    {
                        return JsonNode.Parse(json) as JsonObject;
                    }
                    catch
                    {
                        return null;
                    }
                })
                .OfType<JsonObject>()
                
                .ToList();
            return PerformInnerUniqueJoin(rows, joinedTableRows, defaultJoinMatch.Groups[3].Value);
        }
        
        var joinWithKindMatch = JoinWithKindRegex().Match(columns);
        if (joinWithKindMatch.Success)
        {
            var kind = joinWithKindMatch.Groups[1].Value.ToLowerInvariant();
            var joinTable = joinWithKindMatch.Groups[2].Value;
            var column = joinWithKindMatch.Groups[4].Value;
            
            var joinedTableRows = tableLoader(joinTable).Select(json =>
                {
                    try
                    {
                        return JsonNode.Parse(json) as JsonObject;
                    }
                    catch
                    {
                        return null;
                    }
                })
                .OfType<JsonObject>()
                .ToList();

            switch (kind)
            {
                case "innerunique":
                    return PerformInnerUniqueJoin(rows, joinedTableRows, column);
                case "inner":
                    return PerformInnerJoin(rows, joinedTableRows, column);
                case "leftouter":
                    return PerformLeftOuterJoin(rows, joinedTableRows, column);
                case "rightouter":
                    return PerformRightOuterJoin(rows, joinedTableRows, column);
                case "fullouter":
                    return PerformFullOuterJoin(rows, joinedTableRows, column);
            }
        }

        return rows;
    }

    private static List<JsonObject> PerformFullOuterJoin(List<JsonObject> rows, List<JsonObject> joinedTableRows, string column)
    {
        var leftOuter = PerformLeftOuterJoin(rows, joinedTableRows, column);
        var unmatchedRight = joinedTableRows.Where(right =>
            rows.All(left => left[column]?.GetValue<string>() != right[column]?.GetValue<string>()));

        return [.. leftOuter, .. unmatchedRight];
    }

    private static List<JsonObject> PerformRightOuterJoin(List<JsonObject> rows, List<JsonObject> joinedTableRows, string column)
    {
        // Every right row must survive; unmatched right rows have no left counterpart to fall back to.
        return
        [
            .. joinedTableRows.GroupJoin(rows, rightRow => rightRow[column]?.GetValue<string>(),
                    leftRow => leftRow[column]?.GetValue<string>(), (rightRow, matchedLeftRows) => (rightRow, matchedLeftRows))
                .SelectMany(x => x.matchedLeftRows.DefaultIfEmpty(), (x, leftRow) => leftRow ?? x.rightRow)
        ];
    }

    private static List<JsonObject> PerformLeftOuterJoin(List<JsonObject> rows, List<JsonObject> joinedTableRows, string column)
    {
        // Every left row must survive; unmatched left rows have no right counterpart to fall back to.
        return
        [
            .. rows.GroupJoin(joinedTableRows, leftRow => leftRow[column]?.GetValue<string>(),
                    rightRow => rightRow[column]?.GetValue<string>(), (leftRow, matchedRightRows) => (leftRow, matchedRightRows))
                .SelectMany(x => x.matchedRightRows.DefaultIfEmpty(), (x, _) => x.leftRow)
        ];
    }

    private static List<JsonObject> PerformInnerJoin(List<JsonObject> rows, List<JsonObject> joinedTableRows, string column)
    {
        return
        [
            .. rows.Join(joinedTableRows, leftRow => leftRow[column]?.GetValue<string>(),
                rightRow => rightRow[column]?.GetValue<string>(), (leftRow, _) => leftRow)
        ];
    }

    private static List<JsonObject> PerformInnerUniqueJoin(List<JsonObject> rows, List<JsonObject> joinedTableRows, string column)
    {
        return
        [
            .. rows.Join(joinedTableRows, leftRow => leftRow[column]?.GetValue<string>(),
                rightRow => rightRow[column]?.GetValue<string>(), (leftRow, _) => leftRow).Distinct()
        ];
    }

    private static List<JsonObject> ApplySummarize(List<JsonObject> rows, string expression)
    {
        // summarize count() [by bin()]
        var byBinMatch = CountByBinRegex().Match(expression);
        if (byBinMatch.Success)
        {
            var groupField = byBinMatch.Groups[1].Value;
            var roundTo = byBinMatch.Groups[2].Value;
            var groupsByBin = rows.GroupBy(r => RoundTo(r[groupField]?.GetValue<string>(), roundTo));

            // Return only first element of the group and replace the value of the field
            // we're using for bin() with the rounded value
            return
            [
                .. groupsByBin.Select(group =>
                {
                    var first = group.First();
                    first[groupField] = group.Key;
                    return first;
                })
            ];
        }

        // summarize count() [by <field>]
        var byMatch = CountByRegex().Match(expression);
        if (byMatch.Success)
        {
            var groupField = byMatch.Groups[1].Value;
            return
            [
                .. rows
                    .GroupBy(r => r[groupField]?.GetValue<string>() ?? "")
                    .Select(g =>
                    {
                        var obj = new JsonObject
                        {
                            [groupField] = JsonValue.Create(g.Key),
                            ["count_"] = JsonValue.Create(g.Count())
                        };

                        return obj;
                    })
            ];
        }

        // summarize count()
        if (!CountRegex().IsMatch(expression))
        {
            return rows;
        }

        var obj = new JsonObject
        {
            ["count_"] = JsonValue.Create(rows.Count)
        };

        return [obj];
    }

    private static string RoundTo(string? value, string roundTo)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (int.TryParse(value, out var intValue))
        {
            return intValue.ToString();
        }

        if (double.TryParse(value, out var doubleValue))
        {
            return Math.Floor(doubleValue).ToString(CultureInfo.InvariantCulture);
        }

        if (!DateTimeOffset.TryParse(value, out var timestamp))
        {
            throw new InvalidOperationException($"{roundTo} cannot be used as round to for {value}.");
        }

        var interval = ParseKqlTimeSpan(roundTo);
        var bucket = new DateTimeOffset(timestamp.Ticks - timestamp.Ticks % interval.Ticks, TimeSpan.Zero);
        var zeroed = new DateTimeOffset(bucket.Year, bucket.Month, bucket.Day, 0, 0, 0, bucket.Offset);

        return zeroed.ToString("s");
    }

    private static TimeSpan ParseKqlTimeSpan(string literal)
    {
        // There's a special case when a user provides "0" as a timespan literal
        // e.g., ago(0), which in fact means "now"
        if (literal == "0")
        {
            return TimeSpan.FromMilliseconds(0);
        }
        
        var match = TimespanRegex().Match(literal.Trim());
        if (!match.Success)
        {
            throw new FormatException($"Unrecognized timespan literal: '{literal}'");
        }

        var value = double.Parse(match.Groups[1].Value);
        return match.Groups[2].Value.ToLowerInvariant() switch
        {
            "d" => TimeSpan.FromDays(value),
            "h" => TimeSpan.FromHours(value),
            "m" => TimeSpan.FromMinutes(value),
            "s" => TimeSpan.FromSeconds(value),
            "ms" => TimeSpan.FromMilliseconds(value),
            _ => throw new FormatException($"Unrecognized timespan unit in '{literal}'")
        };
    }

    private static List<JsonObject> ApplyOrderBy(List<JsonObject> rows, string expression)
    {
        // order by <field> [asc|desc]
        var match = OrderingRegex().Match(expression);
        if (!match.Success) return rows;

        var field = match.Groups[1].Value;
        var desc = !match.Groups[2].Success || match.Groups[2].Value.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return desc
            ? [.. rows.OrderByDescending(r => r[field]?.GetValue<string>())]
            : [.. rows.OrderBy(r => r[field]?.GetValue<string>())];
    }

    [GeneratedRegex("""^(\w+)\s+startswith\s+"([^"]*)"$""", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex StartsWithRegex();

    [GeneratedRegex("""^(\w+)\s+contains\s+"([^"]*)"$""", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ContainsRegex();

    [GeneratedRegex("""^(\w+)\s*==\s*"([^"]*)"$""")]
    private static partial Regex EqualsRegex();

    [GeneratedRegex("""^(\w+)\s*<\s*"([^"]*)"$""")]
    private static partial Regex LessThanRegex();

    [GeneratedRegex("""^(\w+)\s*<=\s*"([^"]*)"$""")]
    private static partial Regex LowerThanOrEqualRegex();

    [GeneratedRegex("""^(\w+)\s*>\s*"([^"]*)"$""")]
    private static partial Regex GreaterThanRegex();

    [GeneratedRegex("""^(\w+)\s*>=\s*"([^"]*)"$""")]
    private static partial Regex GreaterThanOrEqualRegex();

    [GeneratedRegex(@"^(\w+)\s*(asc|desc)?$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex OrderingRegex();

    [GeneratedRegex(@"^count\(\)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CountRegex();

    [GeneratedRegex(@"^count\(\)\s+by\s+(\w+)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CountByRegex();

    [GeneratedRegex(@"^count\(\)\s+by\s+bin\s*\(\s*(\w+)\s*,\s*([^)]+?)\s*\)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CountByBinRegex();

    [GeneratedRegex(@"^(\d+(?:\.\d+)?)(ms|d|h|m|s)$", RegexOptions.IgnoreCase, "pl-PL")]
    private static partial Regex TimespanRegex();

    [GeneratedRegex(@"ago\s*\(\s*([^)]+?)\s*\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex AgoRegex();
    
    [GeneratedRegex(@"^(\w+)\s+on\s+(\$left\.)?(\w+)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DefaultJoinRegex();
    
    [GeneratedRegex(@"^kind\s*=\s*(\w+)\s+(\w+)\s+on\s+(\$left\.)?(\w+)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex JoinWithKindRegex();
}