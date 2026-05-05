using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// Verifies that Blob, Queue, and Table Storage data-plane endpoints enforce
/// authentication:  Bearer tokens (via AzureLocalCredential) are accepted and
/// anonymous requests (no Authorization header) are rejected with HTTP 401.
/// </summary>
public class StorageBearerAuthTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("B4F18FA2-1A6E-4E13-8B4A-9D3F5A7C2E01");

    private const string SubscriptionName = "sub-storage-bearer-auth";
    private const string ResourceGroupName = "rg-storage-bearer-auth";
    private const string StorageAccountName = "storagebearerauthtest";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "storage", "account", "delete",
            "--name", StorageAccountName, "-g", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()
        ]);
        await Program.RunAsync([
            "storage", "account", "create",
            "--name", StorageAccountName, "-g", ResourceGroupName,
            "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task BlobStorage_WhenBearerTokenIsProvided_ListContainersSucceeds()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var blobUri = new Uri(TopazResourceHelpers.GetBlobServiceUri(StorageAccountName));
        var blobClient = new BlobServiceClient(blobUri, credential);

        // Act
        var containers = new List<string>();
        await foreach (var container in blobClient.GetBlobContainersAsync())
            containers.Add(container.Name);

        // Assert — succeeds without throwing (empty list is fine)
        Assert.Pass("Bearer-authenticated Blob list-containers succeeded.");
    }

    [Test]
    public async Task QueueStorage_WhenBearerTokenIsProvided_ListQueuesSucceeds()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var queueUri = new Uri(TopazResourceHelpers.GetQueueServiceUri(StorageAccountName));
        var queueClient = new QueueServiceClient(queueUri, credential);

        // Act
        var queues = new List<string>();
        await foreach (var queue in queueClient.GetQueuesAsync())
            queues.Add(queue.Name);

        // Assert
        Assert.Pass("Bearer-authenticated Queue list-queues succeeded.");
    }

    [Test]
    public async Task TableStorage_WhenBearerTokenIsProvided_ListTablesSucceeds()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var tableUri = new Uri(TopazResourceHelpers.GetTableServiceUri(StorageAccountName));
        var tableClient = new TableServiceClient(tableUri, credential);

        // Act
        var tables = new List<string>();
        await foreach (var table in tableClient.QueryAsync())
            tables.Add(table.Name);

        // Assert
        Assert.Pass("Bearer-authenticated Table list-tables succeeded.");
    }

    [Test]
    public async Task BlobStorage_WhenNoAuthorizationHeaderIsPresent_Returns401()
    {
        // Arrange — raw HttpClient with no auth header, bypassing the SDK credential
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var httpClient = new HttpClient(handler);
        var url = $"{TopazResourceHelpers.GetBlobServiceUri(StorageAccountName)}?comp=list";

        // Act
        var response = await httpClient.GetAsync(url);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(401),
                "Expected 401 Unauthorized when Authorization header is missing.");
            Assert.That(response.Headers.Contains("WWW-Authenticate"),
                "Expected WWW-Authenticate challenge header in 401 response.");
        });
    }

    [Test]
    public async Task QueueStorage_WhenNoAuthorizationHeaderIsPresent_Returns401()
    {
        // Arrange
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var httpClient = new HttpClient(handler);
        var url = $"{TopazResourceHelpers.GetQueueServiceUri(StorageAccountName)}?comp=list";

        // Act
        var response = await httpClient.GetAsync(url);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(401),
                "Expected 401 Unauthorized when Authorization header is missing.");
            Assert.That(response.Headers.Contains("WWW-Authenticate"),
                "Expected WWW-Authenticate challenge header in 401 response.");
        });
    }
}
