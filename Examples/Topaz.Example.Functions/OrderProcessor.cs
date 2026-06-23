using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Topaz.Example.Functions;

/// <summary>
/// Processes orders arriving on the Service Bus queue.
///
/// Trigger:  Azure Service Bus queue "order-requests"
/// Side effects:
///   - Reads a configuration secret from Azure Key Vault
///   - Upserts a processed-order document to Cosmos DB
///
/// All three services run locally via Topaz — see local.settings.json for
/// the connection strings pointing at Topaz endpoints.
/// </summary>
public class OrderProcessor
{
    private readonly ILogger<OrderProcessor> _logger;
    private readonly SecretClient _secretClient;
    private readonly CosmosClient _cosmosClient;

    public OrderProcessor(
        ILogger<OrderProcessor> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        var keyVaultUri = configuration["KeyVaultUri"]
            ?? throw new InvalidOperationException("KeyVaultUri is not configured.");

        _secretClient = new SecretClient(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());

        var cosmosConnection = configuration["CosmosDbConnection"]
            ?? throw new InvalidOperationException("CosmosDbConnection is not configured.");

        _cosmosClient = new CosmosClient(
            cosmosConnection,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true
            });
    }

    [Function(nameof(OrderProcessor))]
    public async Task Run(
        [ServiceBusTrigger("order-requests", Connection = "ServiceBusConnection")]
        string messageBody)
    {
        _logger.LogInformation("Processing order: {Body}", messageBody);

        // Read processing config from Key Vault
        var configSecret = await _secretClient.GetSecretAsync("ProcessingConfig");
        _logger.LogInformation("Config: {Config}", configSecret.Value.Value);

        // Upsert the processed order into Cosmos DB
        var container = _cosmosClient
            .GetDatabase("orders-db")
            .GetContainer("processed-orders");

        var document = new ProcessedOrder(
            Id: Guid.NewGuid().ToString(),
            Body: messageBody,
            ProcessedAt: DateTimeOffset.UtcNow,
            Config: configSecret.Value.Value);

        await container.UpsertItemAsync(document, new PartitionKey(document.Id));

        _logger.LogInformation("Order written to Cosmos DB: {Id}", document.Id);
    }
}

public record ProcessedOrder(
    [property: Newtonsoft.Json.JsonProperty("id")] string Id,
    string Body,
    DateTimeOffset ProcessedAt,
    string Config);
