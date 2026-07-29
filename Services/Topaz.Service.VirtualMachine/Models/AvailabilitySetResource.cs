using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualMachine.Models.Requests;

namespace Topaz.Service.VirtualMachine.Models;

internal sealed class AvailabilitySetResource : ArmResource<AvailabilitySetResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public AvailabilitySetResource()
#pragma warning restore CS8618
    {
    }

    public AvailabilitySetResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        IDictionary<string, string>? tags,
        AvailabilitySetResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/availabilitySets/{name}";
        Name = name;
        Location = location.ToString();
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Compute/availabilitySets";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override AvailabilitySetResourceProperties Properties { get; init; }
}