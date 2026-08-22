using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class SubscriptionContractResource : ArmSubresource<SubscriptionContractResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SubscriptionContractResource()
#pragma warning restore CS8618
    {
    }

    public SubscriptionContractResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        string productName,
        string name,
        SubscriptionContractResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/products/{productName}/subscriptions{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/products/subscriptions";
    public override SubscriptionContractResourceProperties Properties { get; init; }

    public string GetOwnerId()
    {
        return Properties.OwnerId!.Split('/').Last();
    }
    
    public override string GetParentId()
    {
        var segments = Id.Split("/");
        return segments[9];
    }
}