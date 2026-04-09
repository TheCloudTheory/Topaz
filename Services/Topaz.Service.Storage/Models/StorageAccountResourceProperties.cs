using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models;

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
}
