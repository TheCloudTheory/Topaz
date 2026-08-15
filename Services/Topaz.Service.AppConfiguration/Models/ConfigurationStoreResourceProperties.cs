using JetBrains.Annotations;

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
        return new ConfigurationStoreResourceProperties
        {
            Sku = source?.Sku ?? "Free",
            Endpoint = $"https://{storeName}.azconfig.topaz.local.dev:{Topaz.Shared.GlobalSettings.DefaultAppConfigurationPort}/",
            PublicNetworkAccess = source?.PublicNetworkAccess ?? "Enabled",
            DisableLocalAuth = source?.DisableLocalAuth ?? false,
            CreateMode = source?.CreateMode ?? "Default",
            SoftDeleteRetentionInDays = source?.SoftDeleteRetentionInDays ?? DefaultSoftDeleteRetentionInDays,
            EnablePurgeProtection = source?.EnablePurgeProtection ?? false,
        };
    }
}
