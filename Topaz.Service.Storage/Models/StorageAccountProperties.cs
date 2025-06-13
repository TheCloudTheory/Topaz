using Azure.ResourceManager.Storage.Models;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models;

[UsedImplicitly]
internal record StorageAccountProperties
{
    public AllowedCopyScope? AllowedCopyScope { get; set; }
    public StoragePublicNetworkAccess? PublicNetworkAccess { get; set; }
    public StorageAccountSasPolicy? SasPolicy { get; set; } 
    public StorageCustomDomain? CustomDomain { get; set; }
    public StorageAccountEncryption? Encryption { get; set; }
    public StorageAccountNetworkRuleSet? NetworkRuleSet { get; set; }
    public StorageAccountAccessTier? AccessTier { get; set; }
    public FilesIdentityBasedAuthentication? AzureFilesIdentityBasedAuthentication { get; set; }
    public bool? EnableHttpsTrafficOnly { get; set; }
    public bool? IsSftpEnabled { get; set; }
    public bool? IsLocalUserEnabled { get; set; }
    public bool? IsExtendedGroupEnabled { get; set; }
    public bool? IsHnsEnabled { get; set; }
    public LargeFileSharesState? LargeFileSharesState { get; set; }
    public StorageRoutingPreference? RoutingPreference { get; set; }
    public bool? AllowBlobPublicAccess { get; set; }
    public StorageMinimumTlsVersion? MinimumTlsVersion { get; set; }
    public bool? AllowSharedKeyAccess { get; set; }
    public bool? IsNfsV3Enabled { get; set; }
    public bool? AllowCrossTenantReplication { get; set; }
    public bool? IsDefaultToOAuthAuthentication { get; set; }
    public ImmutableStorageAccount? ImmutableStorageWithVersioning { get; set; }
    public StorageDnsEndpointType? DnsEndpointType { get; set; }
}