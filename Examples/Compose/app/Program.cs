using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Topaz.Identity;
using Topaz.ResourceManager;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Topaz superadmin object ID — grants unrestricted access to all emulated resources.
const string objectId = "00000000-0000-0000-0000-000000000000";
const string subscriptionId = "00000000-0000-0000-0000-000000000001";
const string resourceGroupName = "rg-my-app";
const string keyVaultName = "kv-my-app";
const string storageAccountName = "stmyapp001";

var credential = new AzureLocalCredential(objectId);

// --- Provision Azure resources at startup ------------------------------------
// This mirrors the approach used in Topaz.Example.Dotnet.Web: the app creates
// its own infrastructure via the ARM SDK so no manual provisioning is needed.
// Wait until Topaz is ready (it may still be initialising when this container starts).
using var topazClient = new TopazArmClient(credential);
for (var attempt = 1; ; attempt++)
{
    if (await topazClient.CheckIfReadyAsync()) break;
    if (attempt >= 20) throw new TimeoutException("Topaz did not become ready after 40 seconds.");
    Console.WriteLine($"[startup] Topaz not ready yet (attempt {attempt}/20), retrying in 2 s...");
    await Task.Delay(TimeSpan.FromSeconds(2));
}
await topazClient.CreateSubscriptionAsync(Guid.Parse(subscriptionId), "dev-local");

var armClient = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
var subscription = armClient.GetSubscriptionResource(
    SubscriptionResource.CreateResourceIdentifier(subscriptionId));

// Resource group
var resourceGroups = subscription.GetResourceGroups();
var rgResponse = await resourceGroups.CreateOrUpdateAsync(
    Azure.WaitUntil.Completed,
    resourceGroupName,
    new ResourceGroupData(AzureLocation.WestEurope));
var resourceGroup = rgResponse.Value;

// Key Vault
await resourceGroup.GetKeyVaults().CreateOrUpdateAsync(
    Azure.WaitUntil.Completed,
    keyVaultName,
    new KeyVaultCreateOrUpdateContent(
        AzureLocation.WestEurope,
        new KeyVaultProperties(
            Guid.Empty,
            new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

// Storage Account
await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(
    Azure.WaitUntil.Completed,
    storageAccountName,
    new StorageAccountCreateOrUpdateContent(
        new StorageSku(StorageSkuName.StandardLrs),
        StorageKind.StorageV2,
        AzureLocation.WestEurope));

// --- Build SDK clients pointing at Topaz -------------------------------------
var kvEndpoint = TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName);

// Retrieve the storage account key so we can build the connection string.
var storageAccountResource = await resourceGroup.GetStorageAccountAsync(storageAccountName);
var storageKeys = storageAccountResource.Value.GetKeys().ToArray();
var storageConnectionString = TopazResourceHelpers.GetAzureStorageConnectionString(
    storageAccountName, storageKeys[0].Value);

var secretClient = new SecretClient(kvEndpoint, credential);
var blobServiceClient = new BlobServiceClient(storageConnectionString);

// GET /health
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    keyVault = kvEndpoint.ToString(),
    blobStorage = storageConnectionString
}));

// GET /secrets/{name}
app.MapGet("/secrets/{name}", async (string name) =>
{
    var secret = await secretClient.GetSecretAsync(name);
    return Results.Ok(new { name = secret.Value.Name, value = secret.Value.Value });
});

// PUT /secrets/{name}   body: plain text
app.MapPut("/secrets/{name}", async (string name, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var value = await reader.ReadToEndAsync();
    await secretClient.SetSecretAsync(name, value);
    return Results.NoContent();
});

// PUT /blobs/{container}/{blobName}   body: plain text
app.MapPut("/blobs/{container}/{blobName}", async (string container, string blobName, HttpRequest request) =>
{
    var containerClient = blobServiceClient.GetBlobContainerClient(container);
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(blobName);
    await blobClient.UploadAsync(request.Body, overwrite: true);
    return Results.Created($"/blobs/{container}/{blobName}", null);
});

// GET /blobs/{container}/{blobName}
app.MapGet("/blobs/{container}/{blobName}", async (string container, string blobName) =>
{
    var containerClient = blobServiceClient.GetBlobContainerClient(container);
    var blobClient = containerClient.GetBlobClient(blobName);
    var response = await blobClient.DownloadContentAsync();
    return Results.Text(response.Value.Content.ToString());
});

app.Run();
