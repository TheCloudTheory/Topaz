namespace Topaz.Portal.Models.Insights;

public sealed class ApplicationInsightsQueryResponse
{
    public ApplicationInsightsQueryTable[] Tables { get; init; } = [];
}

public sealed class ApplicationInsightsQueryTable
{
    public string Name { get; init; } = "";
    public ApplicationInsightsQueryColumn[] Columns { get; init; } = [];
    public object?[][] Rows { get; init; } = [];
}

public sealed class ApplicationInsightsQueryColumn
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
}
