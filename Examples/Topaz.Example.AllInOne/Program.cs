using System.Text;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Spectre.Console;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

var builder = WebApplication.CreateBuilder(args);

var storageAccountName = builder.Configuration["Azure:StorageAccountName"]!;
var serviceBusNamespace = builder.Configuration["Azure:ServiceBusNamespace"]!;
var cosmosAccountName = builder.Configuration["Azure:CosmosDbAccountName"]!;
var registryName = builder.Configuration["Azure:ContainerRegistryName"]!;

const string resourceGroupName = "rg-allinone-demo";
const string subscriptionName = "topaz-allinone-demo";

string cosmosConnectionString = string.Empty;
string roleAssignmentId = string.Empty;
string registryLoginServer = string.Empty;

if (builder.Environment.IsDevelopment())
{
    var topazImage = Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "thecloudtheory/topaz-host:nightly";

    var certificateFile = File.ReadAllText("topaz.crt");
    var certificateKey = File.ReadAllText("topaz.key");

    Console.WriteLine($"[Topaz] Starting container: {topazImage}");

    var container = new ContainerBuilder(topazImage)
        .WithPortBinding(443)
        .WithPortBinding(5671)   // AMQPS (Service Bus TLS)
        .WithPortBinding(8889)   // AMQP plain (Service Bus dev emulator)
        .WithPortBinding(8890)   // Storage tables/queues
        .WithPortBinding(8891)   // Blob storage
        .WithPortBinding(8892)   // Container Registry
        .WithPortBinding(8895)   // Cosmos DB
        .WithPortBinding(8899)   // Resource Manager (ARM)
        .WithName("topaz.local.dev")
        .WithResourceMapping(Encoding.UTF8.GetBytes(certificateFile), "/app/topaz.crt")
        .WithResourceMapping(Encoding.UTF8.GetBytes(certificateKey), "/app/topaz.key")
        .WithCommand("--certificate-file", "topaz.crt", "--certificate-key", "topaz.key", "--log-level", "Debug")
        .Build();

    await container.StartAsync().ConfigureAwait(false);
    await Task.Delay(5000);

    var subscriptionId = Guid.NewGuid();
    var credentials = new AzureLocalCredential(Globals.GlobalAdminId);

    Console.WriteLine("[Topaz] Provisioning: Subscription + Resource Group + Storage Account + Service Bus...");

    var envBuilder = await builder.Configuration
        .AddTopaz(subscriptionId, Globals.GlobalAdminId)
        .AddSubscription(subscriptionId, subscriptionName, credentials)
        .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
        .AddStorageAccount(
            ResourceGroupIdentifier.From(resourceGroupName),
            storageAccountName,
            new StorageAccountCreateOrUpdateContent(
                new StorageSku(StorageSkuName.StandardLrs),
                StorageKind.StorageV2,
                AzureLocation.WestEurope))
        .AddServiceBusNamespace(
            ResourceGroupIdentifier.From(resourceGroupName),
            ServiceBusNamespaceIdentifier.From(serviceBusNamespace),
            new ServiceBusNamespaceData(AzureLocation.WestEurope))
        .AddServiceBusQueue(
            ResourceGroupIdentifier.From(resourceGroupName),
            ServiceBusNamespaceIdentifier.From(serviceBusNamespace),
            "orders",
            new ServiceBusQueueData());

    Console.WriteLine("[Topaz] Provisioning: Cosmos DB account + database + container...");

    var armClient = envBuilder.ArmClient;
    var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
    var resourceGroup = (await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false)).Value;

    var cosmosContent = new CosmosDBAccountCreateOrUpdateContent(
        AzureLocation.WestEurope,
        [new CosmosDBAccountLocation { LocationName = "westeurope", FailoverPriority = 0 }])
    {
        Kind = CosmosDBAccountKind.GlobalDocumentDB,
    };

    var cosmosOp = await resourceGroup.GetCosmosDBAccounts()
        .CreateOrUpdateAsync(WaitUntil.Completed, cosmosAccountName, cosmosContent)
        .ConfigureAwait(false);

    var cosmosAccount = cosmosOp.Value;
    var cosmosKeys = await cosmosAccount.GetKeysAsync().ConfigureAwait(false);
    var cosmosPrimaryKey = cosmosKeys.Value.PrimaryMasterKey!;
    cosmosConnectionString = TopazResourceHelpers.GetCosmosDbConnectionString(cosmosAccountName, cosmosPrimaryKey);

    using var cosmosClient = new CosmosClient(
        TopazResourceHelpers.GetCosmosDbAccountEndpoint(cosmosAccountName),
        cosmosPrimaryKey,
        new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, LimitToEndpoint = true });

    await cosmosClient.CreateDatabaseIfNotExistsAsync("orders-db").ConfigureAwait(false);
    await cosmosClient.GetDatabase("orders-db")
        .CreateContainerIfNotExistsAsync("orders", "/id")
        .ConfigureAwait(false);

    Console.WriteLine("[Topaz] Provisioning: Container Registry...");

    var registryData = new ContainerRegistryData(
        AzureLocation.WestEurope,
        new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
    {
        IsAdminUserEnabled = true,
    };

    var registryOp = await resourceGroup.GetContainerRegistries()
        .CreateOrUpdateAsync(WaitUntil.Completed, registryName, registryData)
        .ConfigureAwait(false);

    registryLoginServer = TopazResourceHelpers.GetContainerRegistryLoginServer(registryName);

    Console.WriteLine("[Topaz] Provisioning: RBAC role assignment (Reader on resource group)...");

    // Built-in Reader role definition ID
    const string readerRoleId = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
    var rgScope = resourceGroup.Id.ToString();
    var roleAssignmentName = Guid.NewGuid().ToString();

    var roleAssignmentContent = new RoleAssignmentCreateOrUpdateContent(
        new ResourceIdentifier($"/providers/Microsoft.Authorization/roleDefinitions/{readerRoleId}"),
        Guid.Parse(Globals.GlobalAdminId))
    {
        PrincipalType = RoleManagementPrincipalType.User,
    };

    var roleAssignmentOp = await armClient
        .GetRoleAssignments(new ResourceIdentifier(rgScope))
        .CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentName, roleAssignmentContent)
        .ConfigureAwait(false);

    roleAssignmentId = roleAssignmentOp.Value.Data.Id?.ToString() ?? roleAssignmentName;

    Console.WriteLine();
    var table = new Table()
        .Border(TableBorder.Rounded)
        .Title("[bold yellow]Topaz — All Azure Services Ready Locally[/]")
        .AddColumn("[bold]Service[/]")
        .AddColumn("[bold]Endpoint[/]")
        .AddColumn("[bold]Replaces[/]");

    table.AddRow("Blob Storage",       TopazResourceHelpers.GetBlobServiceUri(storageAccountName),                        "Azurite");
    table.AddRow("Table Storage",      TopazResourceHelpers.GetTableServiceUri(storageAccountName),                       "Azurite");
    table.AddRow("Service Bus",        TopazResourceHelpers.GetServiceBusConnectionStringWithTls(serviceBusNamespace),    "Service Bus Emulator");
    table.AddRow("Cosmos DB",          TopazResourceHelpers.GetCosmosDbAccountEndpoint(cosmosAccountName),                "Cosmos DB Emulator");
    table.AddRow("Container Registry", registryLoginServer,                                                               "N/A — first time available locally");
    table.AddRow("ARM / RBAC",         "https://topaz.local.dev:8899/",                                                  "azure-sdk-for-net live calls");

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine("[grey]RBAC Assignment ID:[/] " + roleAssignmentId);
    Console.WriteLine();

    builder.Services.AddSingleton(new CosmosClient(
        TopazResourceHelpers.GetCosmosDbAccountEndpoint(cosmosAccountName),
        cosmosPrimaryKey,
        new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, LimitToEndpoint = true }));
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// POST /order — write to Cosmos DB, send to Service Bus, upload blob, log to Table Storage
app.MapPost("/order", async ([FromBody] OrderRequest order, IConfiguration config, CosmosClient cosmosClient) =>
{
    var results = new List<string>();

    // 1. Cosmos DB — persist the order document
    var container = cosmosClient.GetContainer("orders-db", "orders");
    await container.UpsertItemAsync(new { id = order.Id, order.Product, order.Quantity, CreatedAt = DateTime.UtcNow });
    results.Add($"cosmos-db: order {order.Id} stored");

    // 2. Service Bus — publish order-created event
    var sbClient = new ServiceBusClient(
        TopazResourceHelpers.GetServiceBusConnectionStringWithTls(config["Azure:ServiceBusNamespace"]!));
    var sender = sbClient.CreateSender("orders");
    await sender.SendMessageAsync(new ServiceBusMessage($"order-created:{order.Id}") { ContentType = "text/plain" });
    await sender.DisposeAsync();
    await sbClient.DisposeAsync();
    results.Add($"service-bus: event published for order {order.Id}");

    // 3. Blob Storage — upload order confirmation
    var blobServiceClient = new BlobServiceClient(
        new Uri(TopazResourceHelpers.GetBlobServiceUri(config["Azure:StorageAccountName"]!)),
        new AzureLocalCredential(Globals.GlobalAdminId));
    var blobContainer = blobServiceClient.GetBlobContainerClient("confirmations");
    await blobContainer.CreateIfNotExistsAsync();
    var blobName = $"order-{order.Id}.json";
    var payload = System.Text.Json.JsonSerializer.Serialize(order);
    await blobContainer.GetBlobClient(blobName).UploadAsync(new BinaryData(payload), overwrite: true);
    results.Add($"blob-storage: confirmation {blobName} uploaded");

    // 4. Table Storage — write audit log entry
    var tableClient = new TableClient(
        new Uri(TopazResourceHelpers.GetTableServiceUri(config["Azure:StorageAccountName"]!)),
        "audit",
        new AzureLocalCredential(Globals.GlobalAdminId));
    await tableClient.CreateIfNotExistsAsync();
    await tableClient.AddEntityAsync(new TableEntity("orders", order.Id)
    {
        ["Product"] = order.Product,
        ["Quantity"] = order.Quantity,
        ["Timestamp"] = DateTimeOffset.UtcNow,
    });
    results.Add($"table-storage: audit entry written for order {order.Id}");

    return Results.Ok(new OrderResult(order.Id, results));
})
.WithName("PlaceOrder")
.WithOpenApi();

// GET /orders — list all orders from Cosmos DB
app.MapGet("/orders", async (CosmosClient cosmosClient) =>
{
    var container = cosmosClient.GetContainer("orders-db", "orders");
    var query = container.GetItemQueryIterator<Newtonsoft.Json.Linq.JObject>("SELECT * FROM c");
    var results = new List<Newtonsoft.Json.Linq.JObject>();
    while (query.HasMoreResults)
    {
        var page = await query.ReadNextAsync();
        results.AddRange(page);
    }
    var json = Newtonsoft.Json.JsonConvert.SerializeObject(results);
    return Results.Text(json, "application/json");
})
.WithName("ListOrders")
.WithOpenApi();

// GET /status — show all provisioned services and what they replaced
app.MapGet("/status", (IConfiguration config) =>
{
    var storageAccount = config["Azure:StorageAccountName"]!;
    var sbNs = config["Azure:ServiceBusNamespace"]!;
    var cosmosAccount = config["Azure:CosmosDbAccountName"]!;
    var registry = config["Azure:ContainerRegistryName"]!;

    return Results.Ok(new
    {
        emulator = "Topaz",
        replaced = new[]
        {
            "Azurite (Azure Storage Emulator)",
            "Service Bus Emulator",
            "Cosmos DB Emulator",
            "No prior ACR emulator existed",
        },
        services = new object[]
        {
            new { name = "Blob Storage",       endpoint = TopazResourceHelpers.GetBlobServiceUri(storageAccount),       replaced = "Azurite" },
            new { name = "Table Storage",      endpoint = TopazResourceHelpers.GetTableServiceUri(storageAccount),      replaced = "Azurite" },
            new { name = "Service Bus",        endpoint = TopazResourceHelpers.GetServiceBusConnectionStringWithTls(sbNs), replaced = "Service Bus Emulator" },
            new { name = "Cosmos DB",          endpoint = TopazResourceHelpers.GetCosmosDbAccountEndpoint(cosmosAccount), replaced = "Cosmos DB Emulator" },
            new { name = "Container Registry", endpoint = TopazResourceHelpers.GetContainerRegistryLoginServer(registry), replaced = "N/A — first time available locally" },
            new { name = "ARM / RBAC",         endpoint = "https://topaz.local.dev:8899/",                               replaced = "azure-sdk-for-net live calls" },
        }
    });
})
.WithName("GetStatus")
.WithOpenApi();

app.Run();

internal record OrderRequest(string Id, string Product, int Quantity);
internal record OrderResult(string OrderId, IEnumerable<string> Steps);
