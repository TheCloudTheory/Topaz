namespace Topaz.Service.Insights.Models;

internal sealed class QueryResult(QueryResultTable[] tables)
{
    public QueryResultTable[] Tables { get; } = tables;
}

internal sealed class QueryResultTable(string name, QueryColumn[] columns, object?[][] rows)
{
    public string Name { get; } = name;
    public QueryColumn[] Columns { get; } = columns;
    public object?[][] Rows { get; } = rows;
}

internal sealed class QueryColumn(string name, string type)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
}
