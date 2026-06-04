using Topaz.Service.CosmosDb.Models.Requests;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlDatabaseResourceProperties
{
    public SqlDatabaseInnerResource Resource { get; set; } = new();
    public SqlDatabaseOptions? Options { get; set; }

    public static SqlDatabaseResourceProperties FromRequest(string databaseName, CreateOrUpdateSqlDatabaseRequest request)
    {
        return new SqlDatabaseResourceProperties
        {
            Resource = SqlDatabaseInnerResource.Create(databaseName),
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

    public static void UpdateFromRequest(SqlDatabaseResourceProperties properties, CreateOrUpdateSqlDatabaseRequest request)
    {
        if (request.Properties?.Options == null)
        {
            return;
        }

        properties.Options ??= new SqlDatabaseOptions();
        properties.Options.Throughput = request.Properties.Options.Throughput ?? properties.Options.Throughput;

        if (request.Properties.Options.AutoscaleSettings != null)
        {
            properties.Options.AutoscaleSettings ??= new SqlDatabaseAutoscaleSettings();
            properties.Options.AutoscaleSettings.MaxThroughput =
                request.Properties.Options.AutoscaleSettings.MaxThroughput;
        }
    }

    public static void UpdateThroughputFromRequest(SqlDatabaseResourceProperties properties, UpdateSqlDatabaseThroughputRequest request)
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
