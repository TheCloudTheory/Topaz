using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models;

public sealed class BackupShortTermRetentionPolicyResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies";
    public BackupShortTermRetentionPolicyProperties Properties { get; init; } = new();

    public static BackupShortTermRetentionPolicyResponse ForDatabase(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        string policyName)
    {
        return new BackupShortTermRetentionPolicyResponse
        {
            Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}" +
                 $"/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}" +
                 $"/backupShortTermRetentionPolicies/{policyName}",
            Name = policyName,
            Properties = new BackupShortTermRetentionPolicyProperties()
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed class BackupShortTermRetentionPolicyProperties
{
    public int RetentionDays { get; init; } = 7;
    public int DiffBackupIntervalInHours { get; init; } = 12;
}
