using System.Text.Json;
using Azure.ResourceManager.Resources.Models;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed class ResourceProviderDataResponse(string providerName)
{
    public string? Id { get; init; }
    public string? Namespace { get; init; } = GetNamespaceFromProviderName(providerName);

    private static string GetNamespaceFromProviderName(string providerName)
    {
        return providerName.Split("/")[0];
    }

    public string? RegistrationState { get; init; }
    public string? RegistrationPolicy { get; init; }

    public IReadOnlyList<ResourceProviderResourceType>? ResourceTypes { get; init; }  =
        new List<ResourceProviderResourceType>
        {
            new("vaults")
        };

    public string? ProviderAuthorizationConsentState { get; init; } =
        Azure.ResourceManager.Resources.Models.ProviderAuthorizationConsentState.Consented.ToString();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public sealed class ResourceProviderResourceType(string resourceType)
    {
        public string? ResourceType { get; init; } = resourceType;
        public IReadOnlyList<string>? Locations { get; init; }
        public IReadOnlyList<ProviderExtendedLocation>? LocationMappings { get; init; }
        public IReadOnlyList<ResourceTypeAlias>? Aliases { get; init; }

        public IReadOnlyList<string> ApiVersions { get; init; } =
        [
            "2025-12-01"
        ];

        public string DefaultApiVersion { get; init; } = "2025-12-01";
        public IReadOnlyList<ZoneMapping>? ZoneMappings { get; init; }
        public IReadOnlyList<ApiProfile>? ApiProfiles { get; init; }
        public string? Capabilities { get; init; }
        public IReadOnlyDictionary<string, string>? Properties { get; init; }
    }
}