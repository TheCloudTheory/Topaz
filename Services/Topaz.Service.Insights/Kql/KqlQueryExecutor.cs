using System.Globalization;
using System.Text.Json.Nodes;
using Topaz.Service.Insights.Models;

namespace Topaz.Service.Insights.Kql;

internal static partial class KqlQueryExecutor
{
    private static readonly HashSet<string> BuiltInEmptyTables =
        new(StringComparer.OrdinalIgnoreCase) { "AzureActivity", "AzureDiagnostics" };
    
    public static QueryResult Execute(string queryText, string workspaceName, Func<string, string, IEnumerable<string>> tableLoader, Func<string, string> workspaceNameResolver, Func<string, string, IEnumerable<string>>? appTableLoader = null)
    {
        queryText = queryText.Trim();
        
        var pipes = queryText.Split('|');
        var tableName = pipes[0].Trim();
        
        // union TableA, TableB or union (TableA | ...), (TableB | ...)
        List<JsonObject> rows;
        if (tableName.StartsWith("union ", StringComparison.OrdinalIgnoreCase))
        {
            var tableNames = tableName["union ".Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());
            rows = [.. tableNames.SelectMany(t =>
            {
                if (t.StartsWith("workspace(", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadWorkspace(t, tableLoader, workspaceNameResolver);
                }

                if (t.StartsWith("app(", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadApp(t, appTableLoader);
                }
                
                return LoadTable(t, workspaceName, tableLoader);
            })];
        }
        else if(tableName.StartsWith("workspace(", StringComparison.OrdinalIgnoreCase))
        {
            rows = [.. LoadWorkspace(tableName, tableLoader, workspaceNameResolver)];
        }
        else if(tableName.StartsWith("app(", StringComparison.OrdinalIgnoreCase))
        {
            rows = [.. LoadApp(tableName, appTableLoader)];
        }
        else
        {
            rows = [.. LoadTable(tableName, workspaceName, tableLoader)];
        }

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
            else if (compiled.StartsWith("extend ", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplyExtend(rows, compiled["extend ".Length..].Trim());
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
                rows = ApplyJoin(rows, compiled["join ".Length..].Trim(),  workspaceName, tableLoader);
            }
            else if(compiled.StartsWith("workspace(", StringComparison.OrdinalIgnoreCase))
            {
                rows = ApplyWorkspace(rows, compiled, tableLoader);
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

    private static List<JsonObject> LoadTable(string tableName,
        string workspaceName,
        Func<string, string, IEnumerable<string>> tableLoader)
    {
        if (BuiltInEmptyTables.Contains(tableName))
            return [];

        return ParseRows(tableLoader(workspaceName, tableName));
    }

    private static List<JsonObject> ParseRows(IEnumerable<string> payloads) =>
    [
        .. payloads.SelectMany(json =>
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
    ];
    
    private static IEnumerable<JsonObject> LoadWorkspace(string workspaceReference,
        Func<string, string, IEnumerable<string>> tableLoader, Func<string, string> workspaceNameResolver)
    {
        var workspaceMatch = WorkspaceRegex().Match(workspaceReference);
        if (!workspaceMatch.Success) return [];
        
        var workspaceId = workspaceMatch.Groups[1].Value;
        var workspaceResourceId = workspaceMatch.Groups[2].Value;
        var tableName = workspaceReference.Replace(workspaceMatch.Captures[0].Value, string.Empty).Split(".")[1];
        var workspaceName = workspaceNameResolver(string.IsNullOrWhiteSpace(workspaceId) ? workspaceResourceId : workspaceId);

        return LoadTable(tableName, workspaceName, tableLoader);
    }

    private static IEnumerable<JsonObject> LoadApp(string appReference,
        Func<string, string, IEnumerable<string>>? appTableLoader)
    {
        var appMatch = AppRegex().Match(appReference);
        if (!appMatch.Success) return [];

        // app() points at another Application Insights component, either by its name or by its
        // fully-qualified resource ID. The component lives in its own resource group and
        // subscription, so the reference is handed to the loader as-is for resolution there.
        var appIdentifier = !string.IsNullOrWhiteSpace(appMatch.Groups["name"].Value)
            ? appMatch.Groups["name"].Value
            : appMatch.Groups["resourceId"].Value;

        var tableSegments = appReference.Replace(appMatch.Captures[0].Value, string.Empty).Split('.');
        if (tableSegments.Length < 2) return [];
        var tableName = tableSegments[1];

        return appTableLoader == null ? [] : ParseRows(appTableLoader(appIdentifier, tableName));
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
        
        var dateTimeMatch = DateTimeRegex().Match(op);
        if (dateTimeMatch.Success)
        {
            op = DateTimeRegex().Replace(op, match =>
            {
                var dateTime = DateTimeOffset.Parse(match.Groups[1].Value);
                return $"\"{dateTime:O}\"";
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
        
        var betweenMatch = BetweenRegex().Match(predicate);
        if (betweenMatch.Success)
        {
            var field = betweenMatch.Groups[1].Value;
            var low = betweenMatch.Groups[2].Value.Trim('"');
            var high = betweenMatch.Groups[3].Value.Trim('"');
            
            return
            [
                .. rows.Where(r =>
                {
                    var val = r[field]!.GetValue<object>().AsComparableObject();
                    return !val.IsLessThan(low) && !val.IsGreaterThan(high);
                })
            ];
        }
        
        var notBetweenMatch = NotBetweenRegex().Match(predicate);
        if (notBetweenMatch.Success)
        {
            var field = notBetweenMatch.Groups[1].Value;
            var low = notBetweenMatch.Groups[2].Value.Trim('"');
            var high = notBetweenMatch.Groups[3].Value.Trim('"');
            
            return
            [
                .. rows.Where(r =>
                {
                    var val = r[field]!.GetValue<object>().AsComparableObject();
                    return val.IsLessThan(low) || val.IsGreaterThan(high);
                })
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
    
    private static List<JsonObject> ApplyJoin(List<JsonObject> rows, string columns, string workspaceName, Func<string, string, IEnumerable<string>> tableLoader)
    {
        var defaultJoinMatch = DefaultJoinRegex().Match(columns);
        if (defaultJoinMatch.Success)
        {
            var joinedTableRows = tableLoader(workspaceName, defaultJoinMatch.Groups[1].Value).Select(json =>
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
            
            var joinedTableRows = tableLoader(workspaceName, joinTable).Select(json =>
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
    
    private static List<JsonObject> ApplyWorkspace(List<JsonObject> rows, string expression, Func<string, string, IEnumerable<string>> tableLoader)
    {
        var workspaceMatch = WorkspaceRegex().Match(expression);
        if (workspaceMatch.Success)
        {
            
        }

        return rows;
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
    
    private static List<JsonObject> ApplyExtend(List<JsonObject> rows, string expression)
    {
        // alias = field or alias = 'literal'
        var extendMatch = ExtendRegex().Match(expression);
        if (!extendMatch.Success)
        {
            return rows;
        }
        
        var alias = extendMatch.Groups[1].Value;
        var valueExpr = extendMatch.Groups[2].Value.Trim();

        return
        [
            .. rows.Select(r =>
            {
                var clone = JsonNode.Parse(r.ToJsonString())!.AsObject();
                if (valueExpr.StartsWith('"') && valueExpr.EndsWith('"'))
                    clone[alias] = JsonValue.Create(valueExpr[1..^1]);
                else if (r[valueExpr] != null)
                    clone[alias] = r[valueExpr]?.DeepClone();
                else
                    clone[alias] = JsonValue.Create(valueExpr);
                return clone;
            })
        ];
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
}