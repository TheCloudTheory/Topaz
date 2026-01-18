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
    public static Uri GetKeyVaultEndpoint(string vaultName) => new($"https://{vaultName}.keyvault.topaz.local.dev:{GlobalSettings.DefaultKeyVaultPort}");

     /// <summary>
    /// Generates an Azure Storage connection string for local development/testing.
    /// </summary>
    /// <param name="storageAccountName">The name of the storage account.</param>
    /// <param name="accountKey">The account key for authentication.</param>
    /// <returns>A connection string configured to use local storage emulator endpoints.</returns>
    /// <remarks>
    /// All endpoints use HTTP protocol for local development.
    /// </remarks>
    public static string GetAzureStorageConnectionString(string storageAccountName, string accountKey) =>
        $"DefaultEndpointsProtocol=http;AccountName={storageAccountName};AccountKey={accountKey};BlobEndpoint=http://{storageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;QueueEndpoint=http://{storageAccountName}.queue.storage.topaz.local.dev:8899;TableEndpoint=http://{storageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort};";

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
}