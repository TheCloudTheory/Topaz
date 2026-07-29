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

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Compute/availabilitySets";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public override AvailabilitySetResourceProperties Properties { get; init; }

    public void UpdateFromRequest(CreateOrUpdateAvailabilitySetRequest request)
    {
        Tags = request.Tags ?? Tags;
        Properties.PlatformFaultDomainCount = request.PlatformFaultDomainCount ?? Properties.PlatformFaultDomainCount;
        Properties.PlatformUpdateDomainCount = request.PlatformUpdateDomainCount ?? Properties.PlatformUpdateDomainCount;
        Properties.ProximityPlacementGroup = request.ProximityPlacementGroup ?? Properties.ProximityPlacementGroup;
        Properties.VirtualMachines = request.VirtualMachines ?? Properties.VirtualMachines;
        Sku = request.Sku?.Convert() ?? Sku;
        Properties.ScheduledEventsPolicy = request.ScheduledEventsPolicy ?? Properties.ScheduledEventsPolicy;
    }
}