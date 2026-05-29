using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Sql.Models;

public sealed class SqlDatabaseResource : ArmSubresource<SqlDatabaseResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SqlDatabaseResource()
#pragma warning restore CS8618
    {
    }

    public SqlDatabaseResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string serverName,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        SqlDatabaseResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}" +
             $"/providers/Microsoft.Sql/servers/{serverName}/databases/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku ?? new ResourceSku { Name = "Basic", Tier = "Basic" };
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Sql/servers/databases";
    public override SqlDatabaseResourceProperties Properties { get; init; }
    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public ResourceSku? Sku { get; init; }
    public string Kind { get; init; } = "v12.0,user";

    public string GetServer() => Id.Split("/")[8];
}
