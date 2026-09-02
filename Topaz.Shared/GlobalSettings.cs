using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Topaz.Shared;

public static class GlobalSettings
{
    /// <summary>
    /// A global configuration for JSON serialization and deserialization in the application.
    /// </summary>
    /// <remarks>
    /// This static instance of <see cref="JsonSerializerOptions"/> defines custom settings, such as:
    /// - Using camelCase for property naming.
    /// - Ignoring property name case during deserialization.
    /// - Relaxed escaping for JSON strings to allow more permissive encodings.
    /// - Ignoring properties with null values when serializing objects.
    /// - Supporting ISO 8601 formatting for <see cref="TimeSpan"/> and nullable <see cref="TimeSpan"/> values through custom converters.
    /// 
    /// It is used across the application for consistent JSON behavior.
    /// </remarks>
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

    /// <summary>
    /// A JSON serializer configuration specifically designed for CLI operations within the application.
    /// </summary>
    /// <remarks>
    /// This instance of <see cref="JsonSerializerOptions"/> provides tailored settings for JSON processing in CLI contexts, including:
    /// - CamelCase naming policy for property names to align with standard conventions.
    /// - Case-insensitive property name matching during deserialization.
    /// - Indented formatting for human-readable JSON outputs.
    /// - Ignoring null values when serializing objects to minimize payload size.
    /// - Using a relaxed JSON escaping encoder for broader character support.
    /// 
    /// It is utilized in CLI-related JSON serialization and deserialization tasks for consistent and predictable behavior.
    /// </remarks>
    public static readonly JsonSerializerOptions JsonOptionsCli = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Defines the default AMQP port used for Event Hub communication within the application.
    /// </summary>
    /// <remarks>
    /// This constant specifies the port number (8888) used for AMQP protocol-based connections to the Event Hub.
    /// It ensures consistent configuration across various components, such as:
    /// - Endpoint definitions for establishing AMQP-based communication.
    /// - Connection string validations in tests to verify correct usage of Event Hub namespaces and endpoints.
    /// 
    /// The value is globally shared and referenced in both runtime configurations and test cases to guarantee uniformity.
    /// </remarks>
    public const ushort DefaultEventHubAmqpPort = 8888;

    /// <summary>
    /// The default AMQP port used for Service Bus communication in the application.
    /// </summary>
    /// <remarks>
    /// This constant value defines the standard port (8889) for establishing AMQP protocol-based
    /// connections with the Service Bus. It is commonly utilized for local development and testing
    /// environments within the Topaz application ecosystem.
    /// 
    /// Usage scenarios:
    /// - As part of constructing Service Bus connection strings.
    /// - To configure endpoints for Service Bus resources.
    /// - To validate Service Bus configurations in unit tests.
    /// 
    /// By consistently using this constant, the application ensures that Service Bus communication
    /// adheres to a predefined port configuration, promoting uniformity and reducing potential misconfigurations.
    /// </remarks>
    public const ushort DefaultServiceBusAmqpPort = 8889;

    /// <summary>
    /// The additional port used for Service Bus communication within the application.
    /// </summary>
    /// <remarks>
    /// This constant defines a specific port (8887) used in Service Bus configurations and endpoint definitions.
    /// It is utilized across various components to establish connections or bind to additional Service Bus listeners.
    /// The value is employed alongside other networking configurations, ensuring consistency in service operations.
    /// </remarks>
    public const ushort AdditionalServiceBusPort = 8887;

    /// <summary>
    /// The default port used for storage services in the application.
    /// </summary>
    /// <remarks>
    /// This constant defines the default port number (8891) for all storage-related services,
    /// including blob, queue, table, and file storage. It ensures unified configuration for
    /// services that rely on local or emulated storage endpoints, facilitating consistency
    /// across storage operations in development or testing environments.
    /// </remarks>
    public const ushort DefaultStoragePort = 8891;
    
    // Legacy per-sub-service constants kept as aliases while callers are migrated.
    // All storage data-plane sub-services now share DefaultStoragePort.
    public const ushort DefaultTableStoragePort = DefaultStoragePort;
    public const ushort DefaultBlobStoragePort = DefaultStoragePort;
    public const ushort DefaultQueueStoragePort = DefaultStoragePort;
    public const ushort DefaultFileStoragePort = DefaultStoragePort;

    /// <summary>
    /// The default port value used for connections to Cosmos DB endpoints in the application.
    /// </summary>
    /// <remarks>
    /// This constant defines the default port (8895) used for accessing Cosmos DB instances.
    /// It is referenced in various components to ensure consistent port usage for Cosmos DB services,
    /// such as account endpoint generation and data-plane communication.
    /// </remarks>
    public const ushort DefaultCosmosDbPort = 8895;

    /// <summary>
    /// The default port number for the Event Hub service used within the Topaz application.
    /// </summary>
    /// <remarks>
    /// This constant defines the port (8897) used by components that interact with the Event Hub service.
    /// It ensures consistency across various modules and configurations that rely on the Event Hub endpoint.
    /// </remarks>
    public const ushort DefaultEventHubPort = 8897;

    /// <summary>
    /// The default network port used to connect to the Key Vault service in the application.
    /// </summary>
    /// <remarks>
    /// This constant represents the default port for Key Vault endpoints. It is typically used in scenarios
    /// where services or utilities require a predefined port value to establish communication with the
    /// Key Vault service. The value ensures consistency across the application by centralizing the port configuration.
    /// 
    /// Examples of usage include constructing endpoint URLs for Key Vault operations or defining allowed ports
    /// for different service endpoints that interact with the Key Vault service.
    /// </remarks>
    public const ushort DefaultKeyVaultPort = 8898;

    /// <summary>
    /// Specifies the default network port number for the Resource Manager service.
    /// </summary>
    /// <remarks>
    /// This constant defines the port used by various application components for communication
    /// with the Resource Manager service. It is primarily utilized in scenarios such as:
    /// - Constructing service endpoint URLs, e.g., health checks, service interactions.
    /// - Defining endpoint configurations for the host, such as allowed ports and protocols.
    /// 
    /// Default value: 8899.
    /// </remarks>
    public const ushort DefaultResourceManagerPort = 8899;

    /// <summary>
    /// The default port number used for HTTPS communication across the application.
    /// </summary>
    /// <remarks>
    /// This constant defines the default port for secure transmissions over HTTPS protocol.
    /// Commonly referenced throughout various components of the application, it ensures consistent
    /// configuration for services requiring secure communication layers. The value is typically set to 443,
    /// which is the standard port for HTTPS traffic.
    /// 
    /// Examples of its usage include:
    /// - Secure endpoints for hosting web servers.
    /// - Internal service-to-service communications.
    /// - Defining protocol-specific ports for API endpoints.
    /// 
    /// By centralizing this configuration, the application maintains a single source of truth
    /// for HTTPS port definitions.
    /// </remarks>
    public const ushort HttpsPort = 443;

    /// <summary>
    /// The port number used for accessing the Container Registry service.
    /// </summary>
    /// <remarks>
    /// This constant defines the default port for communication with the Container Registry service.
    /// It is used across various endpoints and helpers in the application to ensure a consistent
    /// network configuration. The value of this port is immutable and must align with the
    /// infrastructure setup for the Container Registry.
    /// </remarks>
    public const ushort ContainerRegistryPort = 8892;

    /// <summary>
    /// Represents the default port number used for establishing AMQP 1.0 connections over TLS (Transport Layer Security).
    /// </summary>
    /// <remarks>
    /// This constant defines the port value as 5671, which is commonly used for AMQP over TLS in secure communication scenarios.
    /// It is utilized across various components of the application where secure AMQP connections are required, such as:
    /// - AMQP listener initialization in the host to set up endpoints.
    /// - Generating service bus connection strings for secure communication.
    /// - Configuring HTTPS endpoints to avoid port conflicts in specific containerized deployment scenarios.
    /// 
    /// This value ensures consistent configuration for secure message transmission.
    /// </remarks>
    public const ushort AmqpTlsConnectionPort = 5671;
    
    // Unprivileged port for the built-in HTTP CONNECT proxy. Chosen above the registered
    // service port range (1–1023) and unlikely to conflict with common development tools.
    // Follows the same port-constant convention as the other Topaz ports (8887–8899).
    public const ushort ConnectProxyPort = 44380;

    /// <summary>
    /// The hostname used as a base identifier for the Topaz system environment.
    /// </summary>
    /// <remarks>
    /// This constant represents the default hostname for the Topaz platform, configured with a development-specific domain suffix ("topaz.local.dev").
    /// It is widely used across various application components for:
    /// - Identifying services that belong to the Topaz ecosystem.
    /// - Generating URLs and endpoints for Topaz-specific operations.
    /// - Resolving Topaz-related sub-services and routes.
    /// 
    /// This value helps ensure consistency for service discovery and communication within the application.
    /// </remarks>
    public const string TopazHostname = "topaz.local.dev";

    /// <summary>
    /// The main directory name used by the emulator for storing and accessing its resources.
    /// </summary>
    /// <remarks>
    /// This constant specifies the default folder name, ".topaz", which serves as the root directory
    /// for emulator-related files. It is utilized across the application to maintain a consistent
    /// and centralized location for emulator data.
    /// </remarks>
    public const string MainEmulatorDirectory = ".topaz";

    /// <summary>
    /// The DNS suffix used for constructing Key Vault URIs in the Topaz application.
    /// </summary>
    /// <remarks>
    /// This constant defines the domain name suffix for Key Vault services within the application environment.
    /// It is a critical part of the URI used in operations that involve Key Vault resources, ensuring that
    /// they are correctly routed within the infrastructure. Various components and tests construct Key Vault
    /// URIs by appending the vault name and protocol (e.g., "https://") to this suffix.
    /// </remarks>
    public const string KeyVaultDnsSuffix = "vault.topaz.local.dev";

    /// <summary>
    /// The default tenant identifier used throughout the application to represent the primary tenant.
    /// </summary>
    /// <remarks>
    /// This constant provides a standard GUID string value that serves as the default tenant ID. It is utilized in
    /// various components and configurations across the system, including authorization mechanisms,
    /// URL construction, and default object initializations. The value ensures consistency when a specific
    /// tenant ID is not explicitly provided or required.
    /// </remarks>
    public const string DefaultTenantId = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48";

    /// <summary>
    /// The file path to the global DNS entries configuration file used by the application.
    /// </summary>
    /// <remarks>
    /// This variable holds the absolute file path where the global DNS entries are stored as a JSON file.
    /// The configuration file serves as a centralized storage for DNS entries across the system, ensuring consistent
    /// and persistent management of service mappings and related data. The path is constructed relative to the
    /// application's emulator directory (<see cref="GlobalSettings.MainEmulatorDirectory"/>).
    /// 
    /// Typical usage includes reading the file to load existing DNS entries or modifying it to add new ones.
    /// If the file or its parent directory does not exist, it will be created automatically during runtime.
    /// 
    /// This configuration is critical for the application's DNS management features and is accessed in various
    /// contexts such as initialization and service registration.
    /// </remarks>
    public static readonly string GlobalDnsEntriesFilePath = Path.Combine(MainEmulatorDirectory, "global-dns.json");

    /// <summary>
    /// A global setting that specifies the time interval for executing the soft delete purge scheduler.
    /// </summary>
    /// <remarks>
    /// This value determines how frequently the system runs a process to permanently remove items
    /// marked for soft deletion. The interval is defined as a <see cref="TimeSpan"/>, ensuring
    /// consistent timing across the application. Adjusting this value can influence resource
    /// utilization and the timing of cleanup operations.
    /// </remarks>
    public static readonly TimeSpan SoftDeletePurgeSchedulerInterval = TimeSpan.FromHours(1);

    /// <summary>
    /// Constructs the Key Vault host URL based on the provided vault name and the global Key Vault DNS suffix.
    /// </summary>
    /// <param name="vaultName">The name of the Vault for which the host URL is to be constructed.</param>
    /// <returns>A string containing the Key Vault host URL.</returns>
    public static string GetKeyVaultHost(string vaultName) => $"{vaultName.ToLowerInvariant()}.{KeyVaultDnsSuffix}";

    /// <summary>
    /// Constructs the full Key Vault endpoint URL based on the provided vault name and the default Key Vault port.
    /// </summary>
    /// <param name="vaultName">The name of the Vault for which the endpoint URL is to be constructed.</param>
    /// <returns>A string containing the full Key Vault endpoint URL.</returns>
    public static string GetKeyVaultEndpoint(string vaultName) =>
        $"https://{GetKeyVaultHost(vaultName)}:{DefaultKeyVaultPort}";

    public const string DocumentsDnsSuffix = "documents.topaz.local.dev";
    public const string AzureWebsitesDnsSuffix = "azurewebsites.topaz.local.dev";
    public const ushort DefaultAppServiceKuduPort = 8896;
    public const ushort DefaultAppConfigurationPort = 8893;
    public const string AppServiceKuduDnsSuffix = "scm.azurewebsites.topaz.local.dev";
    public const string AppConfigurationDnsSuffix = "azconfig.topaz.local.dev";
    public const string ApplicationInsightsDnsSuffix = "applicationinsights.topaz.local.dev";

    public static string GetAppConfigurationEndpoint(string storeName) =>
        $"https://{storeName}.{AppConfigurationDnsSuffix}:{DefaultAppConfigurationPort}/";
    
    public static string GetApplicationInsightsEndpoint(string componentName) =>
        $"https://{componentName}.{ApplicationInsightsDnsSuffix}:{DefaultResourceManagerPort}/";

    public static string GetWebSiteDefaultHostName(string siteName) => $"{siteName}.{AzureWebsitesDnsSuffix}";

    /// <summary>
    /// The file path to the defaults configuration file used by the application.
    /// </summary>
    /// <remarks>
    /// This static readonly field represents the location of the default settings file,
    /// combining the main emulator directory (<see cref="MainEmulatorDirectory"/>)
    /// and the filename "defaults.json".
    /// 
    /// It is used across the application for reading, writing, and initializing default
    /// configuration values. Components such as the <see cref="DefaultsProvider"/>
    /// rely on this path to load, update, and save default settings.
    /// </remarks>
    public static readonly string DefaultsPath = Path.Combine(MainEmulatorDirectory, "defaults.json");

    /// <summary>
    /// Represents the DNS suffix used for Event Grid endpoints in the application.
    /// </summary>
    /// <remarks>
    /// This constant builds the Event Grid DNS suffix dynamically based on the host name defined in <see cref="TopazHostname"/>.
    /// It is utilized to construct service-specific Event Grid URLs and ensures consistency across the application's integrations with Event Grid.
    /// </remarks>
    public const string EventGridDnsSuffix = $"eventgrid.{TopazHostname}";
}
