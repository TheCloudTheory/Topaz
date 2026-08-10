using System.Text.Json.Serialization;
using Azure.ResourceManager.Resources;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Insights.Models;

public sealed class ApplicationInsightsComponentResource : ArmResource<ApplicationInsightsComponentResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ApplicationInsightsComponentResource()
#pragma warning restore CS8618
    {
    }

    public ApplicationInsightsComponentResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        string? kind,
        ApplicationInsightsComponentResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/microsoft.insights/components/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
        Kind = kind ?? "web";
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "microsoft.insights/components";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public sealed override ResourceSku? Sku { get; set; }
    public sealed override string? Kind { get; init; }
    public sealed override ApplicationInsightsComponentResourceProperties Properties { get; init; }

    public static ApplicationInsightsComponentResource From(GenericResourceData data)
    {
        return new ApplicationInsightsComponentResource
        {
            Location = data.Location,
            Tags = data.Tags,
            Properties =
                data.Properties.ToObjectFromJson<ApplicationInsightsComponentResourceProperties>(GlobalSettings
                    .JsonOptions)!,
            Kind = data.Kind,
        };
    }
}
