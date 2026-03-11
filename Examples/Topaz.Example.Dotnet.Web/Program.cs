using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Storage.Models;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

var builder = WebApplication.CreateBuilder(args);
var keyVaultName = builder.Configuration["Azure:KeyVaultName"]!;
var storageAccountName = builder.Configuration["Azure:StorageAccountName"]!;

if (builder.Environment.IsDevelopment())
{
    var container = new ContainerBuilder("thecloudtheory/topaz-cli:v1.0.450-alpha")
        .WithPortBinding(8890)
        .WithPortBinding(8899)
        .WithPortBinding(8898)
        .WithPortBinding(8897)
        .WithPortBinding(8891)
        .WithName("topaz.local.dev")
        .WithCommand("start", "--log-level", "Debug")
        .Build();

    await container.StartAsync()
        .ConfigureAwait(false);

    await Task.Delay(5000);

    var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
    var subscriptionId = Guid.NewGuid();
    const string resourceGroupName = "rg-topaz-webapp-example";

    await builder.Configuration.AddTopaz(subscriptionId, Globals.GlobalAdminId)
        .AddSubscription(subscriptionId, "topaz-webapp-example", credentials)
        .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
        .AddStorageAccount(ResourceGroupIdentifier.From(resourceGroupName), storageAccountName,
            new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2,
                AzureLocation.WestEurope))
        .AddKeyVault(ResourceGroupIdentifier.From(resourceGroupName), keyVaultName,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))),
            secrets: new Dictionary<string, string>
            {
                { "secrets-generic-secret", "This is just example secret!" }
            }, Globals.GlobalAdminId)
        .AddStorageAccountConnectionStringAsSecret(ResourceGroupIdentifier.From(resourceGroupName), storageAccountName, keyVaultName,
            "connectionstring-storageaccount", Globals.GlobalAdminId);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddAzureKeyVault(
    TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName), new AzureLocalCredential(Globals.GlobalAdminId));

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