namespace Topaz.Portal.Models.Sql;

public sealed class SqlServerDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public string? FullyQualifiedDomainName { get; set; }
    public string? Version { get; set; }
    public string? State { get; set; }
    public string? AdministratorLogin { get; set; }
    public Dictionary<string, string> Tags { get; set; } = [];
}

public sealed class ListSqlServersResponse
{
    public SqlServerDto[] Value { get; set; } = [];
}
