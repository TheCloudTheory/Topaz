namespace Topaz.Service.CosmosDb.Models.Requests;

public sealed class CreateOrUpdateSqlDatabaseRequest
{
    public CreateOrUpdateSqlDatabaseRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateSqlDatabaseRequestProperties
    {
        public CreateOrUpdateSqlDatabaseResourceInfo? Resource { get; set; }
        public SqlDatabaseOptions? Options { get; set; }
    }

    public sealed class CreateOrUpdateSqlDatabaseResourceInfo
    {
        public string? Id { get; set; }
    }
}
