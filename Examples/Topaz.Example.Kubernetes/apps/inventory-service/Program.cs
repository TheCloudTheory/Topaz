using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Queues;
using Topaz.Identity;
using Topaz.ResourceManager;

var subscriptionId = Environment.GetEnvironmentVariable("TOPAZ_SUBSCRIPTION_ID")
    ?? throw new InvalidOperationException("TOPAZ_SUBSCRIPTION_ID is required");

const string objectId = Globals.GlobalAdminId;
const string resourceGroupName = "rg-inventory-service";
const string storageAccountName = "stinventory001";
const string queueName = "inventory-events";

var credential = new AzureLocalCredential(objectId);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

QueueClient? queue = null;

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

    await topazClient.CreateSubscriptionAsync(Guid.Parse(subscriptionId), "inventory-service-sub");

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

    queue = new QueueClient(connectionString, queueName);
    await queue.CreateIfNotExistsAsync();

    Console.WriteLine("[startup] Inventory service ready.");
});

app.MapPost("/items", async (HttpRequest request) =>
{
    if (queue is null) return Results.StatusCode(503);
    using var reader = new System.IO.StreamReader(request.Body);
    var message = await reader.ReadToEndAsync();
    await queue.SendMessageAsync(message);
    return Results.Accepted();
});

app.MapGet("/items", async () =>
{
    if (queue is null) return Results.StatusCode(503);
    // Peek at up to 10 messages without dequeuing them.
    var messages = await queue.PeekMessagesAsync(maxMessages: 10);
    var items = messages.Value.Select(m => m.MessageText).ToArray();
    return Results.Ok(items);
});

app.Run();
