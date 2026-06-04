namespace Topaz.Service.CosmosDb.Models.Requests;

public sealed class UpdateSqlDatabaseThroughputRequest
{
    public UpdateSqlDatabaseThroughputRequestProperties? Properties { get; set; }

    public sealed class UpdateSqlDatabaseThroughputRequestProperties
    {
        public SqlDatabaseThroughputUpdateResource? Resource { get; set; }
    }

    public sealed class SqlDatabaseThroughputUpdateResource
    {
        public int? Throughput { get; set; }
        public SqlDatabaseAutoscaleSettings? AutoscaleSettings { get; set; }
    }
}
