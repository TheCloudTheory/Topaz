using System.Text.Json;

namespace Topaz.Shared;

public static class GlobalSettings
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
         PropertyNameCaseInsensitive = true
    };

    public const ushort DefaultEventHubAmqpPort = 8888;
    public const ushort DefaultTableStoragePort = 8890;
    public const ushort DefaultBlobStoragePort = 8891;
    public const ushort DefaultEventHubPort = 8897;
    public const ushort DefaultKeyVaultPort = 8898;
    public const ushort DefaultResourceManagerPort = 8899;
}
