using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class CreateOrUpdateContainerRegistryRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public ContainerRegistrySku? Sku { get; init; }
    public ContainerRegistryProperties? Properties { get; init; }
    public ResourceIdentityRequest? Identity { get; init; }

    internal static CreateOrUpdateContainerRegistryRequest FromResource(ContainerRegistryResource registry) =>
        new()
        {
            Location = registry.Location,
            Tags = registry.Tags,
            Sku = new ContainerRegistrySku { Name = registry.Sku?.Name ?? "Basic" },
            Properties = new ContainerRegistryProperties
            {
                AdminUserEnabled = registry.Properties.AdminUserEnabled,
                DataEndpointEnabled = registry.Properties.DataEndpointEnabled,
                PublicNetworkAccess = registry.Properties.PublicNetworkAccess,
                ZoneRedundancy = registry.Properties.ZoneRedundancy,
                NetworkRuleBypassOptions = registry.Properties.NetworkRuleBypassOptions
            },
            Identity = registry.Identity != null
                ? new ResourceIdentityRequest { Type = registry.Identity.Type }
                : null
        };

    [UsedImplicitly]
    internal sealed class ContainerRegistrySku
    {
        public string? Name { get; init; }
    }

    [UsedImplicitly]
    internal sealed class ContainerRegistryProperties
    {
        public bool? AdminUserEnabled { get; init; }
        public bool? DataEndpointEnabled { get; init; }
        public string? PublicNetworkAccess { get; init; }
        public string? ZoneRedundancy { get; init; }
        public string? NetworkRuleBypassOptions { get; init; }
    }

    [UsedImplicitly]
    internal sealed class ResourceIdentityRequest
    {
        public string? Type { get; init; }
    }

    public static CreateOrUpdateContainerRegistryRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateContainerRegistryRequest
        {
            Location = data.Location,
            Sku = new ContainerRegistrySku
            {
                Name = data.Sku.Name
            },
            Tags = data.Tags,
            Identity = new ResourceIdentityRequest { Type = data.Identity?.ManagedServiceIdentityType.ToString() },
            Properties = data.Properties.ToObjectFromJson<ContainerRegistryProperties>(GlobalSettings.JsonOptions)
        };
    }
}
