using System.Text.RegularExpressions;

namespace Topaz.Service.Insights.Kql;

internal static partial class KqlQueryExecutor
{
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
    
    [GeneratedRegex(@"datetime\s*\(\s*([^)]+?)\s*\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DateTimeRegex();
    
    [GeneratedRegex(@"^(\w+)\s+on\s+(\$left\.)?(\w+)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DefaultJoinRegex();
    
    [GeneratedRegex(@"^kind\s*=\s*(\w+)\s+(\w+)\s+on\s+(\$left\.)?(\w+)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex JoinWithKindRegex();
    
    [GeneratedRegex(
        """^(\w+)\s+between\s*\(\s*("[^"]*"|[^\s.]+)\s*\.\.\s*("[^"]*"|[^\s.]+)\s*\)$""",
        RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex BetweenRegex();
    
    [GeneratedRegex(
        """^(\w+)\s+!between\s*\(\s*("[^"]*"|[^\s.]+)\s*\.\.\s*("[^"]*"|[^\s.]+)\s*\)$""",
        RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NotBetweenRegex();
    
    [GeneratedRegex(@"^(\w+)\s+between\s+\(([\d.]+)\s*\.\.\s*([\d.]+)\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ExtendRegex();
    
    [GeneratedRegex(@"^workspace\(\s*""(?<guid>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})""\s*\)|workspace\(\s*""(?<resourceId>/subscriptions/[0-9a-fA-F-]+/resourcegroups/[^/]+/providers/Microsoft\.OperationalInsights/workspaces/[^""]+)""\s*\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex WorkspaceRegex();

    [GeneratedRegex(@"^app\(\s*""(?<name>[^""/]+)""\s*\)|app\(\s*""(?<resourceId>/subscriptions/[0-9a-fA-F-]+/resourcegroups/[^/]+/providers/Microsoft\.Insights/components/[^""]+)""\s*\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex AppRegex();
}