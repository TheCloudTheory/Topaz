using System.Text.Json;
using Azure.ResourceManager.Resources.Models;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed class ResourceProviderDataResponse(string providerName)
{
    public const string RegisteredState = "Registered";
    public const string NotRegisteredState = "NotRegistered";
    public const string UnregisteredState = "Unregistered";

    private static readonly Dictionary<string, string[]> KnownResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.KeyVault"]          = ["vaults"],
        ["Microsoft.Authorization"]     = ["roleDefinitions", "roleAssignments"],
        ["Microsoft.ContainerRegistry"] = ["registries"],
        ["Microsoft.Storage"]           = ["storageAccounts"],
        ["Microsoft.ServiceBus"]        = ["namespaces"],
        ["Microsoft.EventHub"]          = ["namespaces"],
        ["Microsoft.Network"]           = ["virtualNetworks", "networkInterfaces", "publicIPAddresses"],
        ["Microsoft.Compute"]           = ["virtualMachines", "disks"],
        ["Microsoft.Resources"]         = ["resourceGroups", "deployments", "subscriptions"],
        ["Microsoft.ManagedIdentity"]   = ["userAssignedIdentities"],
        ["Microsoft.Insights"]          = ["components"],
        ["Microsoft.DocumentDB"]         = ["databaseAccounts"],
        ["Microsoft.AppConfiguration"]  = ["configurationStores"],
    };

    public string? Id { get; init; }
    public string? Namespace { get; init; } = GetNamespaceFromProviderName(providerName);

    private static string GetNamespaceFromProviderName(string providerName)
    {
        return providerName.Split("/")[0];
    }

    public string? RegistrationState { get; init; }
    public string? RegistrationPolicy { get; init; }

    public IReadOnlyList<ResourceProviderResourceType>? ResourceTypes { get; init; } =
        (KnownResourceTypes.TryGetValue(providerName.Split("/")[0], out var types)
            ? types
            : Array.Empty<string>())
        .Select(t => new ResourceProviderResourceType(t))
        .ToList();

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

        public IReadOnlyList<string> ApiVersions { get; init; } = GetApiVersions(resourceType);

        public string DefaultApiVersion { get; init; } = GetDefaultApiVersion(resourceType);
        public IReadOnlyList<ZoneMapping>? ZoneMappings { get; init; }
        public IReadOnlyList<ApiProfile>? ApiProfiles { get; init; }
        public string? Capabilities { get; init; }
        public IReadOnlyDictionary<string, string>? Properties { get; init; }

        private static IReadOnlyList<string> GetApiVersions(string resourceType)
        {
            return resourceType.Equals("disks", StringComparison.OrdinalIgnoreCase)
                ? ["2025-11-01"]
                : ["2025-12-01"];
        }

        private static string GetDefaultApiVersion(string resourceType)
        {
            return resourceType.Equals("disks", StringComparison.OrdinalIgnoreCase)
                ? "2025-11-01"
                : "2025-12-01";
        }
    }
}