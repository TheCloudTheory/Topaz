using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed partial class ReplicaResource : ArmResource<ReplicaResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ReplicaResource()
#pragma warning restore CS8618
    {
    }

    public ReplicaResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string storeName,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ReplicaResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{storeName}/replicas/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.AppConfiguration/configurationStores/replicas";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public sealed override ResourceSku? Sku { get; init; }
    public sealed override string? Kind { get; init; }
    public override ReplicaResourceProperties Properties { get; init; }
    public ReplicaSystemData? SystemData { get; init; }

    public (bool IsValid, string? Error) Validate()
    {
        return !ReplicaNameRegex().IsMatch(Name) ? (false, $"Replica name '{Name}' is invalid. Name must match pattern '^[a-zA-Z0-9]*$'.") : (true, null);
    }

    [UsedImplicitly]
    internal sealed class ReplicaSystemData
    {
        public string? CreatedBy { get; init; }
        public string? CreatedByType { get; init; }
        public DateTimeOffset? CreatedAt { get; init; }
        public string? LastModifiedBy { get; init; }
        public string? LastModifiedByType { get; init; }
        public DateTimeOffset? LastModifiedAt { get; init; }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]*$")]
    private static partial Regex ReplicaNameRegex();
}