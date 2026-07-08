using System.Security.Cryptography;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class ConsistencyPolicySettings
{
    public string DefaultConsistencyLevel { get; set; } = "Session";
    public int? MaxStalenessPrefix { get; set; }
    public int? MaxIntervalInSeconds { get; set; }
}

public sealed class DatabaseAccountLocation
{
    public string? Id { get; set; }
    public string? LocationName { get; set; }
    public int? FailoverPriority { get; set; }
    public bool? IsZoneRedundant { get; set; }
    public string? ProvisioningState { get; set; }
    public string? DocumentEndpoint { get; set; }
}

public sealed class FailoverPolicy
{
    public string? Id { get; set; }
    public string? LocationName { get; set; }
    public int FailoverPriority { get; set; }
}

public sealed class IpAddressOrRange
{
    public string? IpAddressOrRangeValue { get; set; }
}

public sealed class Capability
{
    public string? Name { get; set; }
}

public sealed class ApiProperties
{
    public string? ServerVersion { get; set; }
}

public sealed class PeriodicModeProperties
{
    public int BackupIntervalInMinutes { get; set; } = 240;
    public int BackupRetentionIntervalInHours { get; set; } = 8;
    public string BackupStorageRedundancy { get; set; } = "Geo";
}

public sealed class BackupPolicyModel
{
    public string Type { get; set; } = "Periodic";
    public PeriodicModeProperties? PeriodicModeProperties { get; set; }
}

public sealed class DatabaseAccountResourceProperties
{
    public string Kind { get; set; } = "GlobalDocumentDB";
    public ConsistencyPolicySettings? ConsistencyPolicy { get; set; }
    public DatabaseAccountLocation[] Locations { get; set; } = [];
    public string DatabaseAccountOfferType { get; set; } = "Standard";
    public IpAddressOrRange[] IpRules { get; set; } = [];
    public bool IsVirtualNetworkFilterEnabled { get; set; }
    public bool EnableAutomaticFailover { get; set; }
    public Capability[] Capabilities { get; set; } = [];
    public string PublicNetworkAccess { get; set; } = "Enabled";
    public bool EnableFreeTier { get; set; }
    public bool EnableAnalyticalStorage { get; set; }
    public ApiProperties? ApiProperties { get; set; }
    public BackupPolicyModel BackupPolicy { get; set; } = new() { PeriodicModeProperties = new PeriodicModeProperties() };
    public string ProvisioningState => "Succeeded";
    public string? DocumentEndpoint { get; set; }
    public string? AccountName { get; set; }
    public bool DisableLocalAuth { get; set; }

    public DatabaseAccountLocation[] ReadLocations => Locations
        .Select(l => new DatabaseAccountLocation
        {
            Id = $"{AccountName}-{l.LocationName?.ToLowerInvariant().Replace(" ", "")}",
            LocationName = l.LocationName,
            FailoverPriority = l.FailoverPriority,
            IsZoneRedundant = l.IsZoneRedundant ?? false,
            ProvisioningState = "Succeeded",
            DocumentEndpoint = DocumentEndpoint
        }).ToArray();

    public DatabaseAccountLocation[] WriteLocations => Locations
        .Where(l => (l.FailoverPriority ?? 0) == 0)
        .Select(l => new DatabaseAccountLocation
        {
            Id = $"{AccountName}-{l.LocationName?.ToLowerInvariant().Replace(" ", "")}",
            LocationName = l.LocationName,
            FailoverPriority = l.FailoverPriority,
            IsZoneRedundant = l.IsZoneRedundant ?? false,
            ProvisioningState = "Succeeded",
            DocumentEndpoint = DocumentEndpoint
        }).ToArray();

    public FailoverPolicy[] FailoverPolicies => Locations
        .Select(l => new FailoverPolicy
        {
            Id = $"{AccountName}-{l.LocationName?.ToLowerInvariant().Replace(" ", "")}",
            LocationName = l.LocationName,
            FailoverPriority = l.FailoverPriority ?? 0
        }).ToArray();
    public string? PrimaryMasterKey { get; set; }
    public string? SecondaryMasterKey { get; set; }
    public string? PrimaryReadonlyMasterKey { get; set; }
    public string? SecondaryReadonlyMasterKey { get; set; }

    private static DatabaseAccountLocation[] BuildLocations(string accountName, string documentEndpoint, DatabaseAccountLocation[]? locations)
    {
        return (locations ?? []).Select(l => new DatabaseAccountLocation
        {
            Id = $"{accountName}-{l.LocationName?.ToLowerInvariant().Replace(" ", "")}",
            LocationName = l.LocationName,
            FailoverPriority = l.FailoverPriority,
            IsZoneRedundant = l.IsZoneRedundant ?? false,
            ProvisioningState = "Succeeded",
            DocumentEndpoint = documentEndpoint
        }).ToArray();
    }

    public static DatabaseAccountResourceProperties FromRequest(string accountName, CreateOrUpdateDatabaseAccountRequest request)
    {
        var documentEndpoint = $"https://{accountName}.{GlobalSettings.DocumentsDnsSuffix}:{GlobalSettings.DefaultCosmosDbPort}/";
        return new DatabaseAccountResourceProperties
        {
            Kind = request.Kind ?? "GlobalDocumentDB",
            ConsistencyPolicy = request.Properties?.ConsistencyPolicy,
            Locations = BuildLocations(accountName, documentEndpoint, request.Properties?.Locations),
            DatabaseAccountOfferType = request.Properties?.DatabaseAccountOfferType ?? "Standard",
            IpRules = request.Properties?.IpRules ?? [],
            IsVirtualNetworkFilterEnabled = request.Properties?.IsVirtualNetworkFilterEnabled.GetValueOrDefault(false) ?? false,
            EnableAutomaticFailover = request.Properties?.EnableAutomaticFailover.GetValueOrDefault(false) ?? false,
            Capabilities = request.Properties?.Capabilities ?? [],
            PublicNetworkAccess = request.Properties?.PublicNetworkAccess ?? "Enabled",
            EnableFreeTier = request.Properties?.EnableFreeTier.GetValueOrDefault(false) ?? false,
            EnableAnalyticalStorage = request.Properties?.EnableAnalyticalStorage.GetValueOrDefault(false) ?? false,
            ApiProperties = request.Properties?.ApiProperties,
            BackupPolicy = new BackupPolicyModel { PeriodicModeProperties = new PeriodicModeProperties() },
            AccountName = accountName,
            DocumentEndpoint = documentEndpoint,
            PrimaryMasterKey = GenerateMasterKey(),
            SecondaryMasterKey = GenerateMasterKey(),
            PrimaryReadonlyMasterKey = GenerateMasterKey(),
            SecondaryReadonlyMasterKey = GenerateMasterKey(),
            DisableLocalAuth = request.Properties?.DisableLocalAuth ?? false
        };
    }

    public static void UpdateFromRequest(DatabaseAccountResourceProperties properties, CreateOrUpdateDatabaseAccountRequest request)
    {
        if (request.Kind != null)
            properties.Kind = request.Kind;
        if (request.Properties?.ConsistencyPolicy != null)
            properties.ConsistencyPolicy = request.Properties.ConsistencyPolicy;
        if (request.Properties?.Locations != null)
            properties.Locations = BuildLocations(properties.AccountName ?? string.Empty, properties.DocumentEndpoint ?? string.Empty, request.Properties.Locations);
        if (request.Properties?.DatabaseAccountOfferType != null)
            properties.DatabaseAccountOfferType = request.Properties.DatabaseAccountOfferType;
        if (request.Properties?.IpRules != null)
            properties.IpRules = request.Properties.IpRules;
        if (request.Properties?.IsVirtualNetworkFilterEnabled != null)
            properties.IsVirtualNetworkFilterEnabled = request.Properties.IsVirtualNetworkFilterEnabled.Value;
        if (request.Properties?.EnableAutomaticFailover != null)
            properties.EnableAutomaticFailover = request.Properties.EnableAutomaticFailover.Value;
        if (request.Properties?.Capabilities != null)
            properties.Capabilities = request.Properties.Capabilities;
        if (request.Properties?.PublicNetworkAccess != null)
            properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;
        if (request.Properties?.EnableFreeTier != null)
            properties.EnableFreeTier = request.Properties.EnableFreeTier.Value;
        if (request.Properties?.EnableAnalyticalStorage != null)
            properties.EnableAnalyticalStorage = request.Properties.EnableAnalyticalStorage.Value;
        if (request.Properties?.ApiProperties != null)
            properties.ApiProperties = request.Properties.ApiProperties;
        if (request.Properties?.DisableLocalAuth != null)
            properties.DisableLocalAuth = request.Properties.DisableLocalAuth.Value;
    }

    private static string GenerateMasterKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
