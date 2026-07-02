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
         DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
         Converters =
         {
             new Iso8601TimeSpanConverter(),
             new Iso8601NullableTimeSpanConverter()
         }
    };
    
    public static readonly JsonSerializerOptions JsonOptionsCli = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public const ushort DefaultEventHubAmqpPort = 8888;
    public const ushort DefaultServiceBusAmqpPort = 8889;
    public const ushort AdditionalServiceBusPort = 8887;
    public const ushort DefaultStoragePort = 8891;
    // Legacy per-sub-service constants kept as aliases while callers are migrated.
    // All storage data-plane sub-services now share DefaultStoragePort.
    public const ushort DefaultTableStoragePort = DefaultStoragePort;
    public const ushort DefaultBlobStoragePort = DefaultStoragePort;
    public const ushort DefaultQueueStoragePort = DefaultStoragePort;
    public const ushort DefaultFileStoragePort = DefaultStoragePort;
    public const ushort DefaultCosmosDbPort = 8895;
    public const ushort DefaultEventHubPort = 8897;
    public const ushort DefaultKeyVaultPort = 8898;
    public const ushort DefaultResourceManagerPort = 8899;
    public const ushort HttpsPort = 443;
    public const ushort ContainerRegistryPort = 8892;
    public const ushort AmqpTlsConnectionPort = 5671;
    // Unprivileged port for the built-in HTTP CONNECT proxy. Chosen above the registered
    // service port range (1–1023) and unlikely to conflict with common development tools.
    // Follows the same port-constant convention as the other Topaz ports (8887–8899).
    public const ushort ConnectProxyPort = 44380;
    public const string TopazHostname = "topaz.local.dev";
    public const string MainEmulatorDirectory = ".topaz";
    public const string KeyVaultDnsSuffix = "vault.topaz.local.dev";
    public const string DefaultTenantId = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48";

    public static readonly string GlobalDnsEntriesFilePath = Path.Combine(MainEmulatorDirectory, "global-dns.json");

    public static readonly TimeSpan SoftDeletePurgeSchedulerInterval = TimeSpan.FromHours(1);

    public static string GetKeyVaultHost(string vaultName) => $"{vaultName.ToLowerInvariant()}.{KeyVaultDnsSuffix}";

    public static string GetKeyVaultEndpoint(string vaultName) =>
        $"https://{GetKeyVaultHost(vaultName)}:{DefaultKeyVaultPort}";

    public const string DocumentsDnsSuffix = "documents.topaz.local.dev";
    public const string AzureWebsitesDnsSuffix = "azurewebsites.topaz.local.dev";
    public const ushort DefaultAppServiceKuduPort = 8896;
    public const ushort DefaultAppConfigurationPort = 8893;
    public const string AppServiceKuduDnsSuffix = "scm.azurewebsites.topaz.local.dev";
    public const string AppConfigurationDnsSuffix = "azconfig.topaz.local.dev";

    public static string GetAppConfigurationEndpoint(string storeName) =>
        $"https://{storeName}.{AppConfigurationDnsSuffix}:{DefaultAppConfigurationPort}/";

    public static string GetWebSiteDefaultHostName(string siteName) => $"{siteName}.{AzureWebsitesDnsSuffix}";
    public static readonly string DefaultsPath = Path.Combine(MainEmulatorDirectory, "defaults.json");
}
