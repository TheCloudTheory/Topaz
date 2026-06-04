using Topaz.Service.CosmosDb.Models;

namespace Topaz.Service.CosmosDb.Models.Requests;

public sealed class UpdateSqlContainerThroughputRequest
{
    public UpdateSqlContainerThroughputRequestProperties? Properties { get; set; }

    public sealed class UpdateSqlContainerThroughputRequestProperties
    {
        public SqlContainerThroughputUpdateResource? Resource { get; set; }
    }

    public sealed class SqlContainerThroughputUpdateResource
    {
        public int? Throughput { get; set; }
        public SqlDatabaseAutoscaleSettings? AutoscaleSettings { get; set; }
    }
}
