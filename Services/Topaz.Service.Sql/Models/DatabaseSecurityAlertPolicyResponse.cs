using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models;

public sealed class DatabaseSecurityAlertPolicyResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "Microsoft.Sql/servers/databases/securityAlertPolicies";
    public DatabaseSecurityAlertPolicyProperties Properties { get; init; } = new();

    public static DatabaseSecurityAlertPolicyResponse ForDatabase(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        string policyName)
    {
        return new DatabaseSecurityAlertPolicyResponse
        {
            Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}" +
                 $"/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}" +
                 $"/securityAlertPolicies/{policyName}",
            Name = policyName,
            Properties = new DatabaseSecurityAlertPolicyProperties { State = "Disabled" }
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed class DatabaseSecurityAlertPolicyProperties
{
    public string State { get; init; } = "Disabled";
}
