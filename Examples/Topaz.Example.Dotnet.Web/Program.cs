extern alias AzureCore;
using Azure;
using Azure.Data.Tables;
using AzureLocation = AzureCore::Azure.Core.AzureLocation;
using ETag = AzureCore::Azure.ETag;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Storage.Models;
using Microsoft.AspNetCore.Mvc;
using Testcontainers.Topaz;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

TopazContainer? container = null;

var builder = WebApplication.CreateBuilder(args);
var keyVaultName = builder.Configuration["Azure:KeyVaultName"]!;
var storageAccountName = builder.Configuration["Azure:StorageAccountName"]!;
var storeName = builder.Configuration["Azure:StoreName"]!;
var appConfigEndpoint = TopazResourceHelpers.GetAppConfigurationStoreEndpoint(storeName);

if (builder.Environment.IsDevelopment())
{
    container = new TopazBuilder(useNightlyImage: true).Build();

    await container.StartAsync()
        .ConfigureAwait(false);
    
    await Task.Delay(5000);

    var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
    var subscriptionId = Guid.NewGuid();
    const string resourceGroupName = "rg-topaz-webapp-example";

    var resourceGroupIdentifier = ResourceGroupIdentifier.From(resourceGroupName);
    
    await builder.Configuration.AddTopaz(subscriptionId, Globals.GlobalAdminId)
        .AddSubscription(subscriptionId, "topaz-webapp-example", credentials)
        .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
        .AddStorageAccount(resourceGroupIdentifier, storageAccountName,
            new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2,
                AzureLocation.WestEurope))
        .AddKeyVault(resourceGroupIdentifier, keyVaultName,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))),
            secrets: new Dictionary<string, string>
            {
                { "secrets-generic-secret", "This is just example secret!" }
            }, Globals.GlobalAdminId)
        .AddStorageAccountConnectionStringAsSecret(resourceGroupIdentifier, storageAccountName, keyVaultName,
            "connectionstring-storageaccount", Globals.GlobalAdminId)
        .AddConfigurationStore(resourceGroupIdentifier, storeName,
            new AppConfigurationStoreData(AzureLocation.WestEurope, new AppConfigurationSku("Standard")))
        .AddConfigurationStoreReplica(resourceGroupIdentifier, storeName, "ne", new AppConfigurationReplicaData()
        {
            Location = AzureLocation.NorthEurope
        })
        .AddKeyValuesToStore(resourceGroupIdentifier, storeName, "Topaz:numericValue", "12345")
        .AddKeyValuesToStore(resourceGroupIdentifier, storeName, "Topaz:randomValue", Guid.NewGuid().ToString());
}

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.ReplicaDiscoveryEnabled = true;
    options.LoadBalancingEnabled = true;
    options.Connect(new Uri(appConfigEndpoint), new Azure.Identity.DefaultAzureCredential())
        .Select("Topaz:*")
        .ConfigureRefresh(refreshOptions =>
        {
            refreshOptions.RegisterAll();
        })
        .UseFeatureFlags();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddAzureKeyVault(
    TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName), new AzureLocalCredential(Globals.GlobalAdminId));

builder.Services.AddAzureAppConfiguration();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/secret", ([FromQuery] string key, IConfiguration configuration) =>
    {
        // You can call that endpoint as e.g. /secret?key=secrets-generic-secret
        // to see the secret value.
        var secret = configuration.GetValue<string>(key);
        return secret;
    })
    .WithName("GetSecretValue")
    .WithOpenApi();

app.MapGet("/config", (IConfiguration configuration) =>
    {
        var values = configuration.GetSection("Topaz").AsEnumerable();
        return values;
    })
    .WithName("GetConfigValues")
    .WithOpenApi();

app.MapPost("/todoitem", async ([FromBody] ToDoItem item, IConfiguration configuration) =>
    {
        var serviceClient = new TableServiceClient(configuration.GetValue<string>("connectionstring-storageaccount"));
        
        await serviceClient.CreateTableIfNotExistsAsync("testtable");
        
        var tableClient = serviceClient.GetTableClient("testtable");

        await tableClient.AddEntityAsync(new ToDoItemEntity
        {
            Name = item.Name,
            Description = item.Description,
            IsCompleted = item.IsCompleted,
            CreatedBy = item.CreatedBy ?? Globals.GlobalAdminId
        });
    })
    .WithName("AddToDoItem")
    .WithOpenApi();

app.MapGet("/todoitem", async (IConfiguration configuration) =>
    {
        var serviceClient = new TableServiceClient(configuration.GetValue<string>("connectionstring-storageaccount"));

        await serviceClient.CreateTableIfNotExistsAsync("testtable");
        
        var tableClient = serviceClient.GetTableClient("testtable");
        
        var query = tableClient.QueryAsync<ToDoItemEntity>();
        var items = new List<ToDoItem>();

        await foreach (var item in query)
        {
            items.Add(new ToDoItem(item.Name, item.Description, item.IsCompleted, item.CreatedBy));
        }
        
        return items;
        
    })
    .WithName("GetToDoItems")
    .WithOpenApi();

app.Run();

if (container != null)
{
    await container.DisposeAsync();
}

internal record ToDoItem(string? Name, string? Description = null, bool IsCompleted = false, string? CreatedBy = null);

internal record ToDoItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "todoitem";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public string? CreatedBy { get; set; }
}