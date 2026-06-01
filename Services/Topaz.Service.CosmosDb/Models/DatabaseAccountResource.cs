using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.CosmosDb.Models;

public sealed class DatabaseAccountResource : ArmResource<DatabaseAccountResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public DatabaseAccountResource()
#pragma warning restore CS8618
    {
    }

    public DatabaseAccountResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        IDictionary<string, string>? tags,
        DatabaseAccountResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{name}";
        Name = name;
        Location = location.ToString();
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.DocumentDB/databaseAccounts";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; } = "GlobalDocumentDB";
    public sealed override DatabaseAccountResourceProperties Properties { get; init; }
}
