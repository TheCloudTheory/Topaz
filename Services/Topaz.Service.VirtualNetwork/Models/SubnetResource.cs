using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class SubnetResource : ArmSubresource<SubnetResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SubnetResource()
#pragma warning restore CS8618
    {
    }

    public SubnetResource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualNetworkName,
        string subnetName,
        SubnetResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}/subnets/{subnetName}";
        Name = subnetName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Network/virtualNetworks/subnets";
    public override SubnetResourceProperties Properties { get; init; }

    public string GetVirtualNetworkName() => Id.Split("/")[8];
}
