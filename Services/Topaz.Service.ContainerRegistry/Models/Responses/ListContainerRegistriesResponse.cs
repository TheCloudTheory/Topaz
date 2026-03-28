using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class ListContainerRegistriesResponse
{
    public ContainerRegistry[]? Value { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextLink { get; init; }

    public record ContainerRegistry
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Type { get; init; }
        public string? Location { get; init; }
        public IDictionary<string, string>? Tags { get; init; }
        public RegistrySku? Sku { get; init; }
        public RegistryProperties? Properties { get; init; }

        public static ContainerRegistry From(ContainerRegistryResource resource)
        {
            return new ContainerRegistry
            {
                Id = resource.Id,
                Name = resource.Name,
                Type = resource.Type,
                Location = resource.Location,
                Tags = resource.Tags,
                Sku = resource.Sku != null ? new RegistrySku { Name = resource.Sku.Name, Tier = resource.Sku.Name } : null,
                Properties = new RegistryProperties
                {
                    LoginServer = resource.Properties.LoginServer,
                    CreationDate = resource.Properties.CreationDate,
                    ProvisioningState = resource.Properties.ProvisioningState,
                    AdminUserEnabled = resource.Properties.AdminUserEnabled,
                    PublicNetworkAccess = resource.Properties.PublicNetworkAccess,
                    ZoneRedundancy = resource.Properties.ZoneRedundancy,
                    NetworkRuleBypassOptions = resource.Properties.NetworkRuleBypassOptions
                }
            };
        }
    }

    public record RegistrySku
    {
        public string? Name { get; init; }
        public string? Tier { get; init; }
    }

    public record RegistryProperties
    {
        public string? LoginServer { get; init; }
        public DateTimeOffset? CreationDate { get; init; }
        public string? ProvisioningState { get; init; }
        public bool AdminUserEnabled { get; init; }
        public string? PublicNetworkAccess { get; init; }
        public string? ZoneRedundancy { get; init; }
        public string? NetworkRuleBypassOptions { get; init; }
    }

    public override string ToString() =>
        System.Text.Json.JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
