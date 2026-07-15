using Topaz.Shared;

namespace Topaz.ResourceManager;

public static class TopazResourceHelpers
{
    /// <summary>
    /// Gets the Key Vault endpoint URI for a specified vault name.
    /// </summary>
    /// <param name="vaultName">The name of the Key Vault instance.</param>
    /// <returns>A URI pointing to the local Key Vault emulator endpoint for the specified vault.</returns>
    /// <remarks>
    /// The endpoint uses the localhost address with the default Key Vault port defined in GlobalSettings.
    /// </remarks>
    public static Uri GetKeyVaultEndpoint(string vaultName) => new(GlobalSettings.GetKeyVaultEndpoint(vaultName));

     /// <summary>
    /// Generates an Azure Storage connection string for local development/testing.
    /// </summary>
    /// <param name="storageAccountName">The name of the storage account.</param>
    /// <param name="accountKey">The account key for authentication.</param>
    /// <returns>A connection string configured to use local storage emulator endpoints.</returns>
    /// <remarks>
    /// Blob endpoint uses plain HTTP. Queue and Table Storage endpoints use HTTPS (the Topaz certificate covers *.queue.storage.topaz.local.dev and *.table.storage.topaz.local.dev).
    /// </remarks>
    public static string GetAzureStorageConnectionString(string storageAccountName, string accountKey) =>
        $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};BlobEndpoint=https://{storageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/;QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/;TableEndpoint=https://{storageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/;";

    /// <summary>
    /// Gets the Service Bus connection string for the local development emulator.
    /// </summary>
    /// <returns>A connection string configured to connect to Topaz on port 8889.</returns>
    /// <remarks>
    /// The connection string uses:
    /// - Root management shared access key for authentication
    /// - Development emulator flag set to true
    /// - Default localhost endpoint on port 8889
    /// </remarks>
    public static string GetServiceBusConnectionString(string serviceBusNamespaceName) => $"Endpoint=sb://{serviceBusNamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.DefaultServiceBusAmqpPort};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    /// <summary>
    /// Gets the Service Bus connection string for MassTransit or other clients requiring TLS.
    /// </summary>
    /// <returns>A connection string configured to connect to Topaz on port 5671 with TLS.</returns>
    /// <remarks>
    /// The connection string uses:
    /// - Port 5671 (standard AMQPS port with TLS)
    /// - Root management shared access key for authentication
    /// - No UseDevelopmentEmulator flag (forces TLS usage)
    /// </remarks>
    public static string GetServiceBusConnectionStringWithTls(string serviceBusNamespaceName) => $"Endpoint=sb://{serviceBusNamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;";

    /// <summary>
    /// Gets the Service Bus connection string for management operations.
    /// </summary>
    /// <param name="serviceBusNamespaceName">The name of the Service Bus namespace.</param>
    /// <returns>A connection string configured for management operations.</returns>
    public static string GetServiceBusConnectionStringForManagement(string serviceBusNamespaceName) => $"Endpoint=sb://{serviceBusNamespaceName}.servicebus.topaz.local.dev:{GlobalSettings.AdditionalServiceBusPort};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;";
    
    /// <summary>
    /// Gets the Event Hub connection string for the local development emulator.
    /// </summary>
    /// <returns>A connection string configured to connect to Topaz on port 8889.</returns>
    /// <remarks>
    /// The connection string uses:
    /// - Root management shared access key for authentication
    /// - Development emulator flag set to true
    /// - Default localhost endpoint on port 8888
    /// </remarks>
    public static string GetEventHubConnectionString(string eventHubNamespaceName) => $"Endpoint=sb://{eventHubNamespaceName}.eventhub.topaz.local.dev:8888;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    /// <summary>
    /// Gets the Container Registry login server host (host:port) for a specified registry name.
    /// </summary>
    /// <param name="registryName">The name of the Container Registry instance.</param>
    /// <returns>A host:port string for the local Container Registry data-plane endpoint.</returns>
    public static string GetContainerRegistryLoginServer(string registryName) =>
        $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";
    
    /// <summary>
    /// Gets the Blob service URI for a specified storage account.
    /// </summary>
    /// <param name="storageAccountName">The name of the storage account.</param>
    /// <returns>A URI string for the local Blob service endpoint.</returns>
    public static string GetBlobServiceUri(string storageAccountName) => $"https://{storageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/";
    
    /// <summary>
    /// Gets the Queue service URI for a specified storage account.
    /// </summary>
    /// <param name="storageAccountName">The name of the storage account.</param>
    /// <returns>A URI string for the local Queue service endpoint.</returns>
    public static string GetQueueServiceUri(string storageAccountName) => $"https://{storageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/";
    
    /// <summary>
    /// Gets the Table service URI for a specified storage account.
    /// </summary>
    /// <param name="storageAccountName">The name of the storage account.</param>
    /// <returns>A URI string for the local Table service endpoint.</returns>
    public static string GetTableServiceUri(string storageAccountName) => $"https://{storageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultStoragePort}/";

    /// <summary>
    /// Gets the Cosmos DB account endpoint URI for a specified account name.
    /// </summary>
    /// <param name="accountName">The name of the Cosmos DB account.</param>
    /// <returns>A URI string for the local Cosmos DB account endpoint.</returns>
    public static string GetCosmosDbAccountEndpoint(string accountName) => $"https://{accountName}.{GlobalSettings.DocumentsDnsSuffix}:{GlobalSettings.DefaultCosmosDbPort}/";

    /// <summary>
    /// Gets the Cosmos DB connection string for a specified account with a primary key.
    /// </summary>
    /// <param name="accountName">The name of the Cosmos DB account.</param>
    /// <param name="primaryKey">The primary master key for the account.</param>
    /// <returns>A connection string configured to connect to the local Cosmos DB emulator.</returns>
    public static string GetCosmosDbConnectionString(string accountName, string primaryKey) => $"AccountEndpoint={GetCosmosDbAccountEndpoint(accountName)};AccountKey={primaryKey};";

    /// <summary>
    /// Gets the Log Analytics data collection (ingestion) endpoint URI for a specified workspace.
    /// </summary>
    /// <param name="workspaceCustomerId">The workspace customer ID (GUID), available from the workspace properties after creation.</param>
    /// <returns>A URI string for the local Log Analytics data collection endpoint.</returns>
    public static string GetLogAnalyticsIngestionEndpoint(string workspaceCustomerId) =>
        $"https://{workspaceCustomerId}.ods.opinsights.topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}";

    /// <summary>
    /// Gets the Application Insights telemetry ingestion endpoint URI for a specified component.
    /// </summary>
    /// <param name="componentName">The name of the Application Insights component.</param>
    /// <returns>A URI string for the local Application Insights ingestion endpoint.</returns>
    public static string GetApplicationInsightsIngestionEndpoint(string componentName) =>
        $"https://{componentName}.applicationinsights.topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}";
}
