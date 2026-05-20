using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServicePlanResource : ArmResource<AppServicePlanResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public AppServicePlanResource() { }
#pragma warning restore CS8618

    public AppServicePlanResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        AppServicePlanResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/serverfarms/{name}";
        Name = name;
        Location = location.ToString();
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Web/serverfarms";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override AppServicePlanResourceProperties Properties { get; init; }
}
