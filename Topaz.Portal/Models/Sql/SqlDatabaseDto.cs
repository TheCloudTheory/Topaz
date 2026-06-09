namespace Topaz.Portal.Models.Sql;

public sealed class SqlDatabaseDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ServerName { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public string? Collation { get; set; }
    public long? MaxSizeBytes { get; set; }
    public string? CurrentServiceObjectiveName { get; set; }
    public Dictionary<string, string> Tags { get; set; } = [];
}

public sealed class ListSqlDatabasesResponse
{
    public SqlDatabaseDto[] Value { get; set; } = [];
}
