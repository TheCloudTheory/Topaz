using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models;

public sealed class BackupLongTermRetentionPolicyResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "Microsoft.Sql/servers/databases/backupLongTermRetentionPolicies";
    public BackupLongTermRetentionPolicyProperties Properties { get; init; } = new();

    public static BackupLongTermRetentionPolicyResponse ForDatabase(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        string policyName)
    {
        return new BackupLongTermRetentionPolicyResponse
        {
            Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}" +
                 $"/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}" +
                 $"/backupLongTermRetentionPolicies/{policyName}",
            Name = policyName,
            Properties = new BackupLongTermRetentionPolicyProperties()
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed class BackupLongTermRetentionPolicyProperties
{
    public string WeeklyRetention { get; init; } = "PT0S";
    public string MonthlyRetention { get; init; } = "PT0S";
    public string YearlyRetention { get; init; } = "PT0S";
    public int WeekOfYear { get; init; } = 0;
}
