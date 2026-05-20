using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServiceSiteResource : ArmResource<AppServiceSiteResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public AppServiceSiteResource() { }
#pragma warning restore CS8618

    public AppServiceSiteResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        IDictionary<string, string>? tags,
        string? kind,
        AppServiceSiteResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/{name}";
        Name = name;
        Location = location.ToString();
        Tags = tags ?? new Dictionary<string, string>();
        Kind = kind;
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Web/sites";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override AppServiceSiteResourceProperties Properties { get; init; }
}
