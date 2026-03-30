using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class CreateOrUpdateContainerRegistryRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public ContainerRegistrySku? Sku { get; init; }
    public ContainerRegistryProperties? Properties { get; init; }
    public ResourceIdentityRequest? Identity { get; init; }

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
}
