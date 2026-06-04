using Topaz.Service.CosmosDb.Models;

namespace Topaz.Service.CosmosDb.Models.Requests;

public sealed class CreateOrUpdateSqlContainerRequest
{
    public CreateOrUpdateSqlContainerRequestProperties? Properties { get; set; }

    public sealed class CreateOrUpdateSqlContainerRequestProperties
    {
        public CreateOrUpdateSqlContainerResourceInfo? Resource { get; set; }
        public SqlDatabaseOptions? Options { get; set; }
    }

    public sealed class CreateOrUpdateSqlContainerResourceInfo
    {
        public string? Id { get; set; }
        public ContainerPartitionKey? PartitionKey { get; set; }
        public object? IndexingPolicy { get; set; }
        public object? UniqueKeyPolicy { get; set; }
        public int? DefaultTtl { get; set; }
    }
}
