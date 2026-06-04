using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlDatabaseThroughputSettingsResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "default";
    public string Type { get; set; } = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/throughputSettings";
    public SqlDatabaseThroughputSettingsProperties Properties { get; set; } = new();

    public static SqlDatabaseThroughputSettingsResponse From(SqlDatabaseResource database)
    {
        return new SqlDatabaseThroughputSettingsResponse
        {
            Id = $"{database.Id}/throughputSettings/default",
            Name = "default",
            Properties = new SqlDatabaseThroughputSettingsProperties
            {
                Resource = new SqlDatabaseThroughputResource
                {
                    Throughput = database.Properties.Options?.Throughput,
                    AutoscaleSettings = database.Properties.Options?.AutoscaleSettings != null
                        ? new SqlDatabaseAutoscaleSettings
                        {
                            MaxThroughput = database.Properties.Options.AutoscaleSettings.MaxThroughput
                        }
                        : null
                }
            }
        };
    }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed class SqlDatabaseThroughputSettingsProperties
{
    public SqlDatabaseThroughputResource Resource { get; set; } = new();
}

public sealed class SqlDatabaseThroughputResource
{
    public int? Throughput { get; set; }
    public SqlDatabaseAutoscaleSettings? AutoscaleSettings { get; set; }
}
