namespace Topaz.Service.AppConfiguration.Models;

public sealed class ConfigurationStoreResourceProperties
{
    public string? Sku { get; set; }

    public string ProvisioningState => "Succeeded";

    public string? Endpoint { get; set; }

    public string? PublicNetworkAccess { get; set; }

    public bool? DisableLocalAuth { get; set; }

    public string? CreateMode { get; set; }

    public int? SoftDeleteRetentionInDays { get; set; }

    public bool? EnablePurgeProtection { get; set; }

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
            SoftDeleteRetentionInDays = source?.SoftDeleteRetentionInDays ?? 7,
            EnablePurgeProtection = source?.EnablePurgeProtection ?? false,
        };
    }
}
