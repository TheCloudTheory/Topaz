using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Sql.Models;

public sealed class SqlServerConnectionPolicySubresource
    : ArmSubresource<SqlServerConnectionPolicySubresourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SqlServerConnectionPolicySubresource()
#pragma warning restore CS8618
    {
    }

    public SqlServerConnectionPolicySubresource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName,
        string policyName,
        SqlServerConnectionPolicySubresourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}" +
             $"/providers/Microsoft.Sql/servers/{serverName}/connectionPolicies/{policyName}";
        Name = policyName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Sql/servers/connectionPolicies";
    public override SqlServerConnectionPolicySubresourceProperties Properties { get; init; }

    public string GetServer() => Id.Split("/")[8];
}
