using Azure.Storage.Blobs;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class BlobStorageTests
{
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            Guid.Empty.ToString()
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            Guid.Empty.ToString(),
            "--name",
            "sub-test"
        ]);

        await Program.Main([
            "storage",
            "account",
            "delete",
            "--name",
            "devstoreaccount1"
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            "rg-test"
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            "rg-test",
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString()
        ]);

        await Program.Main([
            "storage",
            "account",
            "create",
            "--name",
            "devstoreaccount1",
            "-g",
            "rg-test",
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString()
        ]);
    }

    [Test]
    public void BlobStorageTests_WhenNewBlobContainerIsRequested_ItShouldBeCreated()
    {
        // Arrange
        var blobClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));

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
        var blobClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));

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
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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