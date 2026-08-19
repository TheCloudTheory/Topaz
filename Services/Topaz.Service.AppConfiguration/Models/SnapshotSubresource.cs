using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class SnapshotSubresource : ArmSubresource<SnapshotSubresourceProperties>, IValidatable
{
    internal const long DefaultRetentionPeriod = 2_592_000;

    /// <summary>
    /// Individual snapshot cannot exceed 1MB in size.
    /// </summary>
    internal const long MaximumSizeOfSnapshotInBytes = 1024 * 1024 * 1024;
    
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
    
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return Properties.Size is > MaximumSizeOfSnapshotInBytes ? (false, "Individual snapshot size cannot exceed 1MB.") : (true, null);
    }
}