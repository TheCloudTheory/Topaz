using Topaz.Service.CosmosDb.Models.Requests;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlContainerResourceProperties
{
    public SqlContainerInnerResource Resource { get; set; } = new();
    public SqlDatabaseOptions? Options { get; set; }

    public static SqlContainerResourceProperties FromRequest(string containerName, CreateOrUpdateSqlContainerRequest request)
    {
        var resource = request.Properties?.Resource;
        return new SqlContainerResourceProperties
        {
            Resource = SqlContainerInnerResource.Create(
                containerName,
                resource?.PartitionKey,
                resource?.IndexingPolicy,
                resource?.UniqueKeyPolicy,
                resource?.DefaultTtl),
            Options = request.Properties?.Options != null
                ? new SqlDatabaseOptions
                {
                    Throughput = request.Properties.Options.Throughput,
                    AutoscaleSettings = request.Properties.Options.AutoscaleSettings != null
                        ? new SqlDatabaseAutoscaleSettings
                        {
                            MaxThroughput = request.Properties.Options.AutoscaleSettings.MaxThroughput
                        }
                        : null
                }
                : null
        };
    }

    public static void UpdateFromRequest(SqlContainerResourceProperties properties, CreateOrUpdateSqlContainerRequest request)
    {
        var resource = request.Properties?.Resource;
        if (resource != null)
        {
            if (resource.PartitionKey != null)
                properties.Resource.PartitionKey = resource.PartitionKey;
            if (resource.IndexingPolicy != null)
                properties.Resource.IndexingPolicy = resource.IndexingPolicy;
            if (resource.UniqueKeyPolicy != null)
                properties.Resource.UniqueKeyPolicy = resource.UniqueKeyPolicy;
            if (resource.DefaultTtl.HasValue)
                properties.Resource.DefaultTtl = resource.DefaultTtl;
        }

        if (request.Properties?.Options == null)
            return;

        properties.Options ??= new SqlDatabaseOptions();
        properties.Options.Throughput = request.Properties.Options.Throughput ?? properties.Options.Throughput;

        if (request.Properties.Options.AutoscaleSettings != null)
        {
            properties.Options.AutoscaleSettings ??= new SqlDatabaseAutoscaleSettings();
            properties.Options.AutoscaleSettings.MaxThroughput =
                request.Properties.Options.AutoscaleSettings.MaxThroughput;
        }
    }

    public static void UpdateThroughputFromRequest(SqlContainerResourceProperties properties, UpdateSqlContainerThroughputRequest request)
    {
        properties.Options ??= new SqlDatabaseOptions();
        properties.Options.Throughput = request.Properties?.Resource?.Throughput ?? properties.Options.Throughput;

        if (request.Properties?.Resource?.AutoscaleSettings != null)
        {
            properties.Options.AutoscaleSettings ??= new SqlDatabaseAutoscaleSettings();
            properties.Options.AutoscaleSettings.MaxThroughput =
                request.Properties.Resource.AutoscaleSettings.MaxThroughput;
        }
    }
}
