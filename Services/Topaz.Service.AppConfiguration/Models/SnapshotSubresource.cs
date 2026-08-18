using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class SnapshotSubresource : ArmSubresource<SnapshotSubresourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SnapshotSubresource()
#pragma warning restore CS8618
    {
    }

    public SnapshotSubresource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        SnapshotSubresourceProperties properties)
    {
        Id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.AppConfiguration/configurationStores/snapshots";
    public override SnapshotSubresourceProperties Properties { get; init; }
}