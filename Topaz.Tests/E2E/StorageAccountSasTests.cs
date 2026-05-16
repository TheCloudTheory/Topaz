using Azure;
using Azure.Data.Tables;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Sas;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

/// <summary>
/// End-to-end tests for Account SAS token validation on Blob, Queue, and Table data-plane
/// endpoints. Each test uses <see cref="AccountSasBuilder"/> to generate an Account SAS via
/// the Azure SDK (HMAC-SHA256 with the real account key), then issues a data-plane request
/// with no Authorization header so the security providers must validate via the SAS params.
/// </summary>
public class StorageAccountSasTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private const string SubscriptionName  = "sub-account-sas-tests";
    private const string ResourceGroupName = "rg-account-sas-tests";
    private const string StorageAccountName = "acctsastests";

    private string _accountKey = null!;

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

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient  = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var sub        = await armClient.GetDefaultSubscriptionAsync();
        var rg         = await sub.GetResourceGroupAsync(ResourceGroupName);
        var sa         = await rg.Value.GetStorageAccountAsync(StorageAccountName);
        _accountKey    = sa.Value.GetKeys().First().Value;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds an Account SAS token string using the Azure SDK's <see cref="AccountSasBuilder"/>.
    /// The resulting token string can be wrapped in <see cref="AzureSasCredential"/> and attached
    /// to any service-specific client endpoint.
    /// </summary>
    private string BuildAccountSasToken(
        AccountSasServices services,
        AccountSasResourceTypes resourceTypes,
        AccountSasPermissions permissions,
        DateTimeOffset expiresOn)
    {
        var sasBuilder = new AccountSasBuilder
        {
            Services      = services,
            ResourceTypes = resourceTypes,
            ExpiresOn     = expiresOn
        };
        sasBuilder.SetPermissions(permissions);
        var sharedKeyCredential = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        return sasBuilder.ToSasQueryParameters(sharedKeyCredential).ToString();
    }

    // -----------------------------------------------------------------------
    // Blob — container-level (srt=c, ss=b)
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_AccountSas_Container_GrantsListBlobsAccess()
    {
        // Arrange — upload a blob using the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var blobService = new BlobServiceClient(connectionString);
        blobService.CreateBlobContainer("acct-sas-list");
        blobService.GetBlobContainerClient("acct-sas-list").GetBlobClient("hello.txt")
            .Upload(new BinaryData("hello"));

        // Generate Account SAS: Blob service, Container resource type, Read+List permissions.
        var sasToken = BuildAccountSasToken(
            AccountSasServices.Blobs,
            AccountSasResourceTypes.Container,
            AccountSasPermissions.Read | AccountSasPermissions.List,
            DateTimeOffset.UtcNow.AddHours(1));

        // Act — list blobs using Account SAS (no Authorization header).
        var containerEndpoint = new Uri(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/acct-sas-list");
        var sasContainerClient = new BlobContainerClient(containerEndpoint, new AzureSasCredential(sasToken));
        var blobs = sasContainerClient.GetBlobs().ToArray();

        // Assert
        Assert.That(blobs, Has.Length.EqualTo(1));
        Assert.That(blobs[0].Name, Is.EqualTo("hello.txt"));
    }

    // -----------------------------------------------------------------------
    // Blob — object-level (srt=o, ss=b)
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_AccountSas_Object_GrantsReadAccess()
    {
        // Arrange — upload a blob using the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var blobService = new BlobServiceClient(connectionString);
        blobService.CreateBlobContainer("acct-sas-read");
        blobService.GetBlobContainerClient("acct-sas-read").GetBlobClient("content.txt")
            .Upload(new BinaryData("account sas content"));

        // Generate Account SAS: Blob service, Object resource type, Read permission.
        var sasToken = BuildAccountSasToken(
            AccountSasServices.Blobs,
            AccountSasResourceTypes.Object,
            AccountSasPermissions.Read,
            DateTimeOffset.UtcNow.AddHours(1));

        // Act — download using Account SAS.
        var blobEndpoint = new Uri(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/acct-sas-read/content.txt");
        var sasBlobClient = new Azure.Storage.Blobs.BlobClient(blobEndpoint, new AzureSasCredential(sasToken));
        var downloaded = sasBlobClient.DownloadContent().Value;

        // Assert
        Assert.That(downloaded.Content.ToString(), Is.EqualTo("account sas content"));
    }

    // -----------------------------------------------------------------------
    // Blob — expired token
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_AccountSas_Expired_ReturnsUnauthorized()
    {
        // Arrange — create a container.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var blobService = new BlobServiceClient(connectionString);
        blobService.CreateBlobContainer("acct-sas-expired");

        // Generate an already-expired Account SAS.
        var sasToken = BuildAccountSasToken(
            AccountSasServices.Blobs,
            AccountSasResourceTypes.Container,
            AccountSasPermissions.Read | AccountSasPermissions.List,
            DateTimeOffset.UtcNow.AddDays(-1));

        // Act & Assert — Topaz must reject with 401/403.
        var containerEndpoint = new Uri(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/acct-sas-expired");
        var sasContainerClient = new BlobContainerClient(containerEndpoint, new AzureSasCredential(sasToken));
        Assert.Catch<Exception>(() => sasContainerClient.GetBlobs().ToArray());
    }

    // -----------------------------------------------------------------------
    // Queue — object-level (srt=o, ss=q)
    // -----------------------------------------------------------------------

    [Test]
    public void Queue_AccountSas_GrantsSendAndReceiveAccess()
    {
        // Arrange — create a queue using the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var queueService = new QueueServiceClient(connectionString);
        queueService.CreateQueue("acct-sas-queue");

        // Generate Account SAS: Queue service, Object resource type, Add+Read+Process permissions.
        var sasToken = BuildAccountSasToken(
            AccountSasServices.Queues,
            AccountSasResourceTypes.Object,
            AccountSasPermissions.Add | AccountSasPermissions.Read | AccountSasPermissions.Process,
            DateTimeOffset.UtcNow.AddHours(1));

        // Act — send a message using Account SAS (path /acct-sas-queue/messages → Object level).
        var queueEndpoint = new Uri(
            $"https://{StorageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/acct-sas-queue");
        var sasQueueClient = new QueueClient(queueEndpoint, new AzureSasCredential(sasToken));
        sasQueueClient.SendMessage("hello from account sas");

        // Receive the message.
        var messages = sasQueueClient.ReceiveMessages(maxMessages: 1).Value;

        // Assert
        Assert.That(messages, Has.Length.EqualTo(1));
        Assert.That(messages[0].MessageText, Is.EqualTo("hello from account sas"));
    }

    // -----------------------------------------------------------------------
    // Table — object-level (srt=o, ss=t)
    // -----------------------------------------------------------------------

    [Test]
    public void Table_AccountSas_GrantsQueryAccess()
    {
        // Arrange — create a table and insert an entity using the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableService = new TableServiceClient(connectionString);
        tableService.CreateTable("AcctSasTable");
        tableService.GetTableClient("AcctSasTable")
            .AddEntity(new TableEntity("pk", "rk") { ["Value"] = "account-sas" });

        // Generate Account SAS: Table service, Object resource type, Read permission.
        var sasToken = BuildAccountSasToken(
            AccountSasServices.Tables,
            AccountSasResourceTypes.Object,
            AccountSasPermissions.Read,
            DateTimeOffset.UtcNow.AddHours(1));

        // Act — query entities using Account SAS.
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/AcctSasTable");
        var sasTableClient = new TableClient(tableEndpoint, new AzureSasCredential(sasToken));
        var entities = sasTableClient.Query<TableEntity>().ToArray();

        // Assert
        Assert.That(entities, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(entities[0].PartitionKey, Is.EqualTo("pk"));
            Assert.That(entities[0].RowKey, Is.EqualTo("rk"));
            Assert.That(entities[0]["Value"].ToString(), Is.EqualTo("account-sas"));
        });
    }
}
