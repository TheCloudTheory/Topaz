using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

public sealed class ConfigurationStoreResourceProperties
{
    private const int DefaultSoftDeleteRetentionInDays = 7;
    
    public string? Sku { get; init; }
    [UsedImplicitly] public string ProvisioningState => "Succeeded";
    public string? Endpoint { get; set; }
    public string? PublicNetworkAccess { get; set; }
    public bool? DisableLocalAuth { get; init; }
    public string? CreateMode { get; init; }
    public int? SoftDeleteRetentionInDays { get; init; }
    public bool? EnablePurgeProtection { get; init; }

    public static ConfigurationStoreResourceProperties FromRequest(
        ConfigurationStoreResourceProperties? source,
        string storeName)
    {
        var sku = source?.Sku ?? "Free";
        
        return new ConfigurationStoreResourceProperties
        {
            Sku = sku,
            Endpoint = GlobalSettings.GetAppConfigurationEndpoint(storeName),
            PublicNetworkAccess = source?.PublicNetworkAccess ?? "Enabled",
            DisableLocalAuth = source?.DisableLocalAuth ?? false,
            CreateMode = source?.CreateMode ?? "Default",
            SoftDeleteRetentionInDays = ConfigureSoftDeleteRetentionInDays(source, sku),
            EnablePurgeProtection = ConfigurePurgeProtection(source, sku),
        };
    }

    private static bool? ConfigurePurgeProtection(ConfigurationStoreResourceProperties? source, string sku)
    {
        if (sku == "Free")
        {
            return null;
        }
        
        return source?.EnablePurgeProtection ?? false;
    }

    private static int? ConfigureSoftDeleteRetentionInDays(ConfigurationStoreResourceProperties? source, string sku)
    {
        if (sku == "Free")
        {
            return null;
        }
        
        return source?.SoftDeleteRetentionInDays ?? DefaultSoftDeleteRetentionInDays;
    }
}
