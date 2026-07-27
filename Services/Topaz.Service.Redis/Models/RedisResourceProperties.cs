using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Redis.Models;

internal sealed class RedisResourceProperties
{
    public string ProvisioningState { get; init; } = "Succeeded";
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 6379;
    public int SslPort { get; set; } = 6380;
    public bool EnableNonSslPort { get; set; }
    public bool DisableAccessKeyAuthentication { get; set; }
    public string? MinimumTlsVersion { get; set; }
    public string? PublicNetworkAccess { get; set; } = "Enabled";
    public string? RedisVersion { get; set; } = "latest";
    public int? ReplicasPerMaster { get; set; }
    public int? ReplicasPerPrimary { get; set; }
    public int? ShardCount { get; set; }
    public string? StaticIP { get; set; }
    public string? SubnetId { get; set; }
    public string? UpdateChannel { get; set; }
    public string? ZonalAllocationPolicy { get; set; }
    public IDictionary<string, string>? TenantSettings { get; set; }
    public RedisConfigurationProperties? RedisConfiguration { get; set; }
    public RedisAccessKeys? AccessKeys { get; init; }
    public IList<RedisInstanceDetails> Instances { get; init; } = [];
    public IList<RedisLinkedServer> LinkedServers { get; init; } = [];

    public void UpdateFromRequest(RedisResourceProperties request)
    {
        EnableNonSslPort = request.EnableNonSslPort;
        DisableAccessKeyAuthentication = request.DisableAccessKeyAuthentication;
        MinimumTlsVersion = request.MinimumTlsVersion ?? MinimumTlsVersion;
        PublicNetworkAccess = request.PublicNetworkAccess ?? PublicNetworkAccess;
        RedisVersion = request.RedisVersion ?? RedisVersion;
        ReplicasPerMaster = request.ReplicasPerMaster ?? ReplicasPerMaster;
        ReplicasPerPrimary = request.ReplicasPerPrimary ?? ReplicasPerPrimary;
        ShardCount = request.ShardCount ?? ShardCount;
        StaticIP = request.StaticIP ?? StaticIP;
        SubnetId = request.SubnetId ?? SubnetId;
        UpdateChannel = request.UpdateChannel ?? UpdateChannel;
        ZonalAllocationPolicy = request.ZonalAllocationPolicy ?? ZonalAllocationPolicy;
        TenantSettings = request.TenantSettings ?? TenantSettings;
        RedisConfiguration = request.RedisConfiguration ?? RedisConfiguration;
    }
}

[UsedImplicitly]
internal sealed class RedisConfigurationProperties
{
    [JsonPropertyName("maxmemory-policy")]
    public string? MaxmemoryPolicy { get; init; }

    [JsonPropertyName("maxmemory-reserved")]
    public string? MaxmemoryReserved { get; init; }

    [JsonPropertyName("maxmemory-delta")]
    public string? MaxmemoryDelta { get; init; }

    [JsonPropertyName("rdb-backup-enabled")]
    public string? RdbBackupEnabled { get; init; }

    [JsonPropertyName("rdb-backup-frequency")]
    public string? RdbBackupFrequency { get; init; }

    [JsonPropertyName("rdb-storage-connection-string")]
    public string? RdbStorageConnectionString { get; init; }

    [JsonPropertyName("aof-backup-enabled")]
    public string? AofBackupEnabled { get; init; }

    [JsonPropertyName("notify-keyspace-events")]
    public string? NotifyKeyspaceEvents { get; init; }
}

[UsedImplicitly]
internal sealed class RedisAccessKeys
{
    public string? PrimaryKey { get; init; }
    public string? SecondaryKey { get; init; }
}

[UsedImplicitly]
internal sealed class RedisInstanceDetails
{
    public bool IsMaster { get; init; }
    public bool IsPrimary { get; init; }
    public int? NonSslPort { get; init; }
    public int SslPort { get; init; }
    public int? ShardId { get; init; }
    public string? Zone { get; init; }
}

[UsedImplicitly]
internal sealed class RedisLinkedServer
{
    public string Id { get; init; } = string.Empty;
}