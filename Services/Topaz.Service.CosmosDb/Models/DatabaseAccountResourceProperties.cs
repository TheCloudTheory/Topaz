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
    public string? LocationName { get; set; }
    public int? FailoverPriority { get; set; }
    public bool? IsZoneRedundant { get; set; }
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
    public string ProvisioningState => "Succeeded";
    public string? DocumentEndpoint { get; set; }
    public string? PrimaryMasterKey { get; set; }
    public string? SecondaryMasterKey { get; set; }
    public string? PrimaryReadonlyMasterKey { get; set; }
    public string? SecondaryReadonlyMasterKey { get; set; }

    public static DatabaseAccountResourceProperties FromRequest(string accountName, CreateOrUpdateDatabaseAccountRequest request)
    {
        return new DatabaseAccountResourceProperties
        {
            Kind = request.Kind ?? "GlobalDocumentDB",
            ConsistencyPolicy = request.Properties?.ConsistencyPolicy,
            Locations = request.Properties?.Locations ?? [],
            DatabaseAccountOfferType = request.Properties?.DatabaseAccountOfferType ?? "Standard",
            IpRules = request.Properties?.IpRules ?? [],
            IsVirtualNetworkFilterEnabled = request.Properties?.IsVirtualNetworkFilterEnabled.GetValueOrDefault(false) ?? false,
            EnableAutomaticFailover = request.Properties?.EnableAutomaticFailover.GetValueOrDefault(false) ?? false,
            Capabilities = request.Properties?.Capabilities ?? [],
            PublicNetworkAccess = request.Properties?.PublicNetworkAccess ?? "Enabled",
            EnableFreeTier = request.Properties?.EnableFreeTier.GetValueOrDefault(false) ?? false,
            EnableAnalyticalStorage = request.Properties?.EnableAnalyticalStorage.GetValueOrDefault(false) ?? false,
            ApiProperties = request.Properties?.ApiProperties,
            DocumentEndpoint = $"https://{accountName}.{GlobalSettings.DocumentsDnsSuffix}:{GlobalSettings.DefaultCosmosDbPort}/",
            PrimaryMasterKey = GenerateMasterKey(),
            SecondaryMasterKey = GenerateMasterKey(),
            PrimaryReadonlyMasterKey = GenerateMasterKey(),
            SecondaryReadonlyMasterKey = GenerateMasterKey()
        };
    }

    public static void UpdateFromRequest(DatabaseAccountResourceProperties properties, CreateOrUpdateDatabaseAccountRequest request)
    {
        if (request.Kind != null)
            properties.Kind = request.Kind;
        if (request.Properties?.ConsistencyPolicy != null)
            properties.ConsistencyPolicy = request.Properties.ConsistencyPolicy;
        if (request.Properties?.Locations != null)
            properties.Locations = request.Properties.Locations;
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
    }

    private static string GenerateMasterKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
