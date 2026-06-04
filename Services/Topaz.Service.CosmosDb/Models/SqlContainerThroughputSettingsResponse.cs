using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlContainerThroughputSettingsResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "default";
    public string Type { get; set; } = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/throughputSettings";
    public SqlDatabaseThroughputSettingsProperties Properties { get; set; } = new();

    public static SqlContainerThroughputSettingsResponse From(SqlContainerResource container)
    {
        return new SqlContainerThroughputSettingsResponse
        {
            Id = $"{container.Id}/throughputSettings/default",
            Name = "default",
            Properties = new SqlDatabaseThroughputSettingsProperties
            {
                Resource = new SqlDatabaseThroughputResource
                {
                    Throughput = container.Properties.Options?.Throughput,
                    AutoscaleSettings = container.Properties.Options?.AutoscaleSettings != null
                        ? new SqlDatabaseAutoscaleSettings
                        {
                            MaxThroughput = container.Properties.Options.AutoscaleSettings.MaxThroughput
                        }
                        : null
                }
            }
        };
    }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
