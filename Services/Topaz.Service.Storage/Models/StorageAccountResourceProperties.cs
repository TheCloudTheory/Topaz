using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models;

[UsedImplicitly]
internal record StorageAccountPrimaryEndpoints
{
    public string? Blob { get; set; }
    public string? Queue { get; set; }
    public string? Table { get; set; }
    public string? File { get; set; }
    public string? Web { get; set; }
    public string? Dfs { get; set; }

    public static StorageAccountPrimaryEndpoints For(string accountName) => new()
    {
        Blob = $"https://{accountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        Queue = $"https://{accountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        Table = $"https://{accountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        File = $"https://{accountName}.file.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        Web = $"https://{accountName}.web.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        Dfs = $"https://{accountName}.dfs.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
    };
}

[UsedImplicitly]
internal record StorageAccountSecondaryEndpoints
{
    public string? Blob { get; set; }
    public string? Queue { get; set; }
    public string? Table { get; set; }
    public string? File { get; set; }

    public static StorageAccountSecondaryEndpoints For(string accountName) => new()
    {
        Blob = $"https://{accountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        Queue = $"https://{accountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        Table = $"https://{accountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
        File = $"https://{accountName}-secondary.file.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/",
    };
}

[UsedImplicitly]
internal record StorageAccountResourceProperties
{
    public string? AllowedCopyScope { get; set; }
    public string? PublicNetworkAccess { get; set; }
    public JsonElement? SasPolicy { get; set; }
    public JsonElement? CustomDomain { get; set; }
    public JsonElement? Encryption { get; set; }
    public JsonElement? NetworkRuleSet { get; set; }
    public string? AccessTier { get; set; }
    public JsonElement? AzureFilesIdentityBasedAuthentication { get; set; }
    public bool? EnableHttpsTrafficOnly { get; set; }
    public bool? IsSftpEnabled { get; set; }
    public bool? IsLocalUserEnabled { get; set; }
    public bool? IsExtendedGroupEnabled { get; set; }
    public bool? IsHnsEnabled { get; set; }
    public string? LargeFileSharesState { get; set; }
    public JsonElement? RoutingPreference { get; set; }
    public bool? AllowBlobPublicAccess { get; set; }
    public string? MinimumTlsVersion { get; set; }
    public bool? AllowSharedKeyAccess { get; set; }
    public bool? IsNfsV3Enabled { get; set; }
    public bool? AllowCrossTenantReplication { get; set; }
    public bool? IsDefaultToOAuthAuthentication { get; set; }
    public JsonElement? ImmutableStorageWithVersioning { get; set; }
    public string? DnsEndpointType { get; set; }
    public string ProvisioningState { get; set; } = "Succeeded";
    public string StatusOfPrimary { get; set; } = "available";
    public string? StatusOfSecondary { get; set; }
    public DateTimeOffset? CreationTime { get; set; }
    public StorageAccountPrimaryEndpoints? PrimaryEndpoints { get; set; }
    public StorageAccountSecondaryEndpoints? SecondaryEndpoints { get; set; }
    public DateTimeOffset? LastGeoSyncTime { get; set; }
}
