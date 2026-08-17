using JetBrains.Annotations;
using Topaz.Service.AppConfiguration.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

public sealed class ConfigurationStoreResourceProperties
{
    private const int DefaultSoftDeleteRetentionInDays = 7;
    
    [UsedImplicitly] public string ProvisioningState => "Succeeded";
    public string? Endpoint { get; set; }
    public string? PublicNetworkAccess { get; set; }
    public bool? DisableLocalAuth { get; set; }
    public string? CreateMode { get; init; }
    public int? SoftDeleteRetentionInDays { get; init; }
    public bool? EnablePurgeProtection { get; set; }

    public static ConfigurationStoreResourceProperties FromRequest(
        ConfigurationStoreResourceProperties? source,
        string sku,
        string storeName)
    {
        return new ConfigurationStoreResourceProperties
        {
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

    public void UpdateFromRequest(ConfigurationStoreResource request, string sku)
    {
        DisableLocalAuth = request.Properties.DisableLocalAuth ?? DisableLocalAuth;
        PublicNetworkAccess = request.Properties.PublicNetworkAccess;

        if (EnablePurgeProtection.HasValue || !request.Properties.EnablePurgeProtection.HasValue) return;

        EnablePurgeProtection = ConfigurePurgeProtection(this, sku);
    }

    public void UpdateFromRequest(UpdateConfigurationStoreRequest request, string sku)
    {
        DisableLocalAuth = request.Properties?.DisableLocalAuth ?? DisableLocalAuth;
        PublicNetworkAccess = request.Properties?.PublicNetworkAccess;
        
        if ((EnablePurgeProtection.HasValue && EnablePurgeProtection.Value) || !request.Properties!.EnablePurgeProtection.HasValue) return;

        EnablePurgeProtection = ConfigurePurgeProtection(request, sku);
    }

    private bool? ConfigurePurgeProtection(UpdateConfigurationStoreRequest request, string sku)
    {
        if (sku == "Free")
        {
            return null;
        }
        
        return request.Properties?.EnablePurgeProtection ?? false;
    }
}
