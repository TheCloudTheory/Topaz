using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceTemplate.Models;

internal sealed class ServiceTemplateServiceResource : ArmResource<ServiceTemplateServiceResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ServiceTemplateServiceResource()
#pragma warning restore CS8618
    {
    }

    public ServiceTemplateServiceResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        ServiceTemplateServiceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceTemplate/CHANGEIT/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ServiceTemplate/CHANGEIT";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public override ServiceTemplateServiceResourceProperties Properties { get; init; }
}