namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlDatabaseOptions
{
    public int? Throughput { get; set; }
    public SqlDatabaseAutoscaleSettings? AutoscaleSettings { get; set; }
}
