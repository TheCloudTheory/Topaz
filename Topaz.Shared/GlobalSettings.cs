using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Topaz.Shared;

public static class GlobalSettings
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
         PropertyNameCaseInsensitive = true,
         Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    
    public static readonly JsonSerializerOptions JsonOptionsCli = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public const ushort DefaultEventHubAmqpPort = 8888;
    public const ushort DefaultServiceBusAmqpPort = 8889;
    public const ushort DefaultTableStoragePort = 8890;
    public const ushort DefaultBlobStoragePort = 8891;
    public const ushort DefaultEventHubPort = 8897;
    public const ushort DefaultKeyVaultPort = 8898;
    public const ushort DefaultResourceManagerPort = 8899;
}
