using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class BlobStorageTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "devstoreaccount1";

    private string _key = null!;
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscriptionId",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "storage",
            "account",
            "delete",
            "--name",
            StorageAccountName
        ]);

        await Program.Main([
            "storage",
            "account",
            "create",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString()
        ]);
        
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
        var keys = storageAccount.Value.GetKeys().ToArray();

        _key = keys[0].Value;
    }

    [Test]
    public void BlobStorageTests_WhenNewBlobContainerIsRequested_ItShouldBeCreated()
    {
        // Arrange
        var blobClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

        // Act
        var response = blobClient.CreateBlobContainer("test");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Value.Name, Is.EqualTo("test"));
    }

    [Test]
    public void BlobStorageTests_WhenNewBlobContainersAreCreated_TheyShouldBeCreatedAndAvailable()
    {
        // Arrange
        var blobClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

        // Act
        blobClient.CreateBlobContainer("test");
        blobClient.CreateBlobContainer("test2");
        blobClient.CreateBlobContainer("test3");

        var containers = blobClient.GetBlobContainers().ToArray();

        // Assert
        Assert.That(containers, Has.Length.EqualTo(3));
    }

    [Test]
    public void BlobStorageTests_WhenContainerBlobsAreListed_TheyShouldReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("test");
        var containerClient = serviceClient.GetBlobContainerClient("test");
        var blobClient = containerClient.GetBlobClient("test.txt");
        blobClient.Upload(new BinaryData("some content"));

        // Act
        var blobs = containerClient.GetBlobs().ToArray();

        // Assert
        Assert.That(blobs, Has.Length.EqualTo(1));
    }

    [Test]
    public void BlobStorageTests_WhenBlobPropertiesAreRequested_TheyShouldReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("test");
        var containerClient = serviceClient.GetBlobContainerClient("test");
        var blobClient = containerClient.GetBlobClient("test.txt");
        blobClient.Upload(new BinaryData("some content"));

        // Act
        var properties = blobClient.GetProperties();

        // Assert
        Assert.That(properties, Is.Not.Null);
    }

    [Test]
    public void BlobStorageTests_WhenBlobMetadataAreSet_TheyShouldAccepted()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("test");
        var containerClient = serviceClient.GetBlobContainerClient("test");
        var blobClient = containerClient.GetBlobClient("test.txt");
        blobClient.Upload(new BinaryData("some content"));

        // Act
        var info = blobClient.SetMetadata(new Dictionary<string, string>()
        {
            { "foo", "bar" }
        });

        // Assert
        Assert.That(info, Is.Not.Null);
    }
}