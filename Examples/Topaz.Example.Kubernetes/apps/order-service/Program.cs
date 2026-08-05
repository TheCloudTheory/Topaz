using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using Topaz.Identity;
using Topaz.ResourceManager;

var subscriptionId = Environment.GetEnvironmentVariable("TOPAZ_SUBSCRIPTION_ID")
    ?? throw new InvalidOperationException("TOPAZ_SUBSCRIPTION_ID is required");

const string objectId = Globals.GlobalAdminId;
const string resourceGroupName = "rg-order-service";
const string storageAccountName = "storders001";
const string containerName = "orders";

var credential = new AzureLocalCredential(objectId);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Wait for Topaz then provision resources once at startup.
BlobContainerClient? ordersContainer = null;

_ = Task.Run(async () =>
{
    using var topazClient = new TopazArmClient(credential);
    for (var attempt = 1; ; attempt++)
    {
        if (await topazClient.CheckIfReadyAsync()) break;
        if (attempt >= 30) throw new TimeoutException("Topaz did not become ready.");
        Console.WriteLine($"[startup] Topaz not ready (attempt {attempt}/30), retrying...");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    await topazClient.CreateSubscriptionAsync(Guid.Parse(subscriptionId), "order-service-sub");

    var armClient = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
    var subscription = armClient.GetSubscriptionResource(
        SubscriptionResource.CreateResourceIdentifier(subscriptionId));

    var rg = (await subscription.GetResourceGroups().CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        resourceGroupName,
        new ResourceGroupData(AzureLocation.WestEurope))).Value;

    await rg.GetStorageAccounts().CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        storageAccountName,
        new StorageAccountCreateOrUpdateContent(
            new StorageSku(StorageSkuName.StandardLrs),
            StorageKind.StorageV2,
            AzureLocation.WestEurope));

    var storageAccount = await rg.GetStorageAccountAsync(storageAccountName);
    var key = storageAccount.Value.GetKeys().First().Value;
    var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(storageAccountName, key);

    ordersContainer = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
    await ordersContainer.CreateIfNotExistsAsync();

    Console.WriteLine("[startup] Order service ready.");
});

app.MapGet("/orders", async () =>
{
    if (ordersContainer is null) return Results.StatusCode(503);
    var orders = new List<string>();
    await foreach (var blob in ordersContainer.GetBlobsAsync())
        orders.Add(blob.Name);
    return Results.Ok(orders);
});

app.MapPut("/orders/{id}", async (string id, HttpRequest request) =>
{
    if (ordersContainer is null) return Results.StatusCode(503);
    var blobClient = ordersContainer.GetBlobClient(id);
    await blobClient.UploadAsync(request.Body, overwrite: true);
    return Results.Created($"/orders/{id}", null);
});

app.MapGet("/orders/{id}", async (string id) =>
{
    if (ordersContainer is null) return Results.StatusCode(503);
    var blobClient = ordersContainer.GetBlobClient(id);
    if (!await blobClient.ExistsAsync()) return Results.NotFound();
    var download = await blobClient.DownloadContentAsync();
    return Results.Text(download.Value.Content.ToString());
});

app.Run();
