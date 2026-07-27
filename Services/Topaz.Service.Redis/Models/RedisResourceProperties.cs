using System.Text.Json.Serialization;

namespace Topaz.Service.Redis.Models;

internal sealed class RedisResourceProperties
{
    public string ProvisioningState { get; init; } = "Succeeded";
    public string HostName { get; init; } = string.Empty;
    public int Port { get; init; } = 6379;
    public int SslPort { get; init; } = 6380;
    public bool EnableNonSslPort { get; init; } = false;
    public bool DisableAccessKeyAuthentication { get; init; } = false;
    public string? MinimumTlsVersion { get; init; }
    public string? PublicNetworkAccess { get; init; } = "Enabled";
    public string? RedisVersion { get; init; }
    public int? ReplicasPerMaster { get; init; }
    public int? ReplicasPerPrimary { get; init; }
    public int? ShardCount { get; init; }
    public string? StaticIP { get; init; }
    public string? SubnetId { get; init; }
    public string? UpdateChannel { get; init; }
    public string? ZonalAllocationPolicy { get; init; }
    public IDictionary<string, string>? TenantSettings { get; init; }
    public RedisConfigurationProperties? RedisConfiguration { get; init; }
    public RedisAccessKeys? AccessKeys { get; init; }
    public IList<RedisInstanceDetails> Instances { get; init; } = [];
    public IList<RedisLinkedServer> LinkedServers { get; init; } = [];

    public RedisResourceProperties UpdateFromRequest(RedisResourceProperties request)
    {
        return new RedisResourceProperties
        {
            HostName = HostName,
            Port = Port,
            SslPort = SslPort,
            EnableNonSslPort = request.EnableNonSslPort,
            DisableAccessKeyAuthentication = request.DisableAccessKeyAuthentication,
            MinimumTlsVersion = request.MinimumTlsVersion ?? MinimumTlsVersion,
            PublicNetworkAccess = request.PublicNetworkAccess ?? PublicNetworkAccess,
            RedisVersion = request.RedisVersion ?? RedisVersion,
            ReplicasPerMaster = request.ReplicasPerMaster ?? ReplicasPerMaster,
            ReplicasPerPrimary = request.ReplicasPerPrimary ?? ReplicasPerPrimary,
            ShardCount = request.ShardCount ?? ShardCount,
            StaticIP = request.StaticIP ?? StaticIP,
            SubnetId = request.SubnetId ?? SubnetId,
            UpdateChannel = request.UpdateChannel ?? UpdateChannel,
            ZonalAllocationPolicy = request.ZonalAllocationPolicy ?? ZonalAllocationPolicy,
            TenantSettings = request.TenantSettings ?? TenantSettings,
            RedisConfiguration = request.RedisConfiguration ?? RedisConfiguration,
            AccessKeys = AccessKeys,
            Instances = Instances,
            LinkedServers = LinkedServers
        };
    }
}

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

internal sealed class RedisAccessKeys
{
    public string? PrimaryKey { get; init; }
    public string? SecondaryKey { get; init; }
}

internal sealed class RedisInstanceDetails
{
    public bool IsMaster { get; init; }
    public bool IsPrimary { get; init; }
    public int? NonSslPort { get; init; }
    public int SslPort { get; init; }
    public int? ShardId { get; init; }
    public string? Zone { get; init; }
}

internal sealed class RedisLinkedServer
{
    public string Id { get; init; } = string.Empty;
}