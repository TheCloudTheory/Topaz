using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class BlobStorageUserDelegationSasTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("B4A1E2D3-F5C6-4789-A012-B3C4D5E6F701");

    private const string SubscriptionName = "sub-udkey-test";
    private const string ResourceGroupName = "test-udkey";
    private const string StorageAccountName = "udkeytest";
    private const string ContainerName = "udkey-container";
    private const string BlobName = "hello.txt";
    private const string BlobContent = "User Delegation SAS test content";

    private string _accountKey = null!;

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["storage", "account", "delete", "--name", StorageAccountName, "-g", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["storage", "account", "create", "--name", StorageAccountName, "-g", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
        _accountKey = storageAccount.Value.GetKeys().First().Value;

        // Upload a blob using SharedKey so it can be retrieved via User Delegation SAS in tests.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync();
        var blobClient = containerClient.GetBlobClient(BlobName);
        await blobClient.UploadAsync(BinaryData.FromString(BlobContent), overwrite: true);
    }

    [Test]
    public async Task UserDelegationKey_Generate_ReturnsValidKey()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var serviceUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}");
        var blobServiceClient = new BlobServiceClient(serviceUri, credential);

        var response = await blobServiceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(DateTimeOffset.UtcNow.AddHours(1))
            {
                StartsOn = DateTimeOffset.UtcNow.AddSeconds(-5)
            });

        Assert.That(response.Value, Is.Not.Null);
        Assert.That(response.Value.SignedObjectId, Is.EqualTo(Globals.GlobalAdminId));
        Assert.That(response.Value.SignedTenantId, Is.Not.Empty);
        Assert.That(response.Value.SignedService, Is.EqualTo("b"));
        Assert.That(response.Value.Value, Is.Not.Empty);
    }

    [Test]
    public async Task UserDelegationSas_Blob_CanDownloadBlob()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var serviceUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}");
        var blobServiceClient = new BlobServiceClient(serviceUri, credential);

        var keyExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(keyExpiry)
            {
                StartsOn = DateTimeOffset.UtcNow.AddSeconds(-5)
            });

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = BlobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddSeconds(-5),
            ExpiresOn = keyExpiry
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasParams = sasBuilder.ToSasQueryParameters(userDelegationKey, StorageAccountName);
        var sasUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/{ContainerName}/{BlobName}?{sasParams}");

        var sasClient = new BlobClient(sasUri);
        var download = await sasClient.DownloadContentAsync();

        Assert.That(download.Value.Content.ToString(), Is.EqualTo(BlobContent));
    }

    [Test]
    public async Task UserDelegationSas_Container_CanListBlobs()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var serviceUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}");
        var blobServiceClient = new BlobServiceClient(serviceUri, credential);

        var keyExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(keyExpiry)
            {
                StartsOn = DateTimeOffset.UtcNow.AddSeconds(-5)
            });

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            Resource = "c",
            StartsOn = DateTimeOffset.UtcNow.AddSeconds(-5),
            ExpiresOn = keyExpiry
        };
        sasBuilder.SetPermissions(BlobContainerSasPermissions.List);

        var sasParams = sasBuilder.ToSasQueryParameters(userDelegationKey, StorageAccountName);
        var sasUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/{ContainerName}?{sasParams}");

        var sasContainerClient = new BlobContainerClient(sasUri);
        var blobs = sasContainerClient.GetBlobsAsync();
        var blobNames = new List<string>();
        await foreach (var blob in blobs)
            blobNames.Add(blob.Name);

        Assert.That(blobNames, Contains.Item(BlobName));
    }

    [Test]
    public async Task UserDelegationSas_WithExpiredToken_ReturnsForbidden()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var serviceUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}");
        var blobServiceClient = new BlobServiceClient(serviceUri, credential);

        // Issue a key with a past expiry.
        var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(DateTimeOffset.UtcNow.AddHours(-1))
            {
                StartsOn = DateTimeOffset.UtcNow.AddHours(-2)
            });

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = BlobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddHours(-2),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(-1) // already expired
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasParams = sasBuilder.ToSasQueryParameters(userDelegationKey, StorageAccountName);
        var sasUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/{ContainerName}/{BlobName}?{sasParams}");

        var sasClient = new BlobClient(sasUri);
        Assert.ThrowsAsync<Azure.RequestFailedException>(async () => await sasClient.DownloadContentAsync());
    }
}
