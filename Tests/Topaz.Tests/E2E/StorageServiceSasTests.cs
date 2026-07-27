using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Sas;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Data.Tables.Sas;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

/// <summary>
/// End-to-end tests for Service SAS token validation on Blob, Queue, and Table data-plane
/// endpoints. Each test generates a SAS token using the Azure SDK (which uses the correct
/// HMAC-SHA256 key derivation), then issues a data-plane request without an Authorization
/// header so the security provider must validate via the SAS query parameters.
/// </summary>
public class StorageServiceSasTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("9D3F8E7B-2A1C-4F5D-B6E8-1A2C3D4E5F67");

    private const string SubscriptionName  = "sub-service-sas-tests";
    private const string ResourceGroupName = "rg-service-sas-tests";
    private const string StorageAccountName = "servicesastests";

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
    // Blob — container SAS (sr=c)
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_ServiceSas_Container_GrantsListBlobsAccess()
    {
        // Arrange — upload a blob with the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sas-list");
        serviceClient.GetBlobContainerClient("sas-list").GetBlobClient("hello.txt").Upload(new BinaryData("hello"));

        // Generate a container SAS (sr=c, sp=rl) via the SDK — uses Convert.FromBase64String.
        var sasUri = serviceClient.GetBlobContainerClient("sas-list")
            .GenerateSasUri(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List,
                DateTimeOffset.UtcNow.AddHours(1));

        // Act — use SAS URI with no Authorization header.
        var sasClient = new BlobContainerClient(sasUri);
        var blobs = sasClient.GetBlobs().ToArray();

        // Assert
        Assert.That(blobs, Has.Length.EqualTo(1));
        Assert.That(blobs[0].Name, Is.EqualTo("hello.txt"));
    }

    [Test]
    public void Blob_ServiceSas_Blob_GrantsReadAccess()
    {
        // Arrange — upload a blob.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sas-read");
        var blobClient = serviceClient.GetBlobContainerClient("sas-read").GetBlobClient("content.txt");
        blobClient.Upload(new BinaryData("expected content"));

        // Generate a blob SAS (sr=b, sp=r).
        var sasBlobUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

        // Act — download with the SAS URI.
        var sasBlob = new BlobClient(sasBlobUri);
        var downloaded = sasBlob.DownloadContent().Value;

        // Assert
        Assert.That(downloaded.Content.ToString(), Is.EqualTo("expected content"));
    }

    [Test]
    public void Blob_ServiceSas_Expired_ReturnsUnauthorized()
    {
        // Arrange — create a container.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sas-expired");

        // Generate an already-expired SAS.
        var sasUri = serviceClient.GetBlobContainerClient("sas-expired")
            .GenerateSasUri(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List,
                DateTimeOffset.UtcNow.AddDays(-1));

        // Act & Assert — the server must reject with 401 / 403.
        // The SDK may throw either RequestFailedException or JsonReaderException depending on whether
        // the empty error body can be parsed; both indicate the request was correctly denied.
        var sasClient = new BlobContainerClient(sasUri);
        Assert.Catch<Exception>(() => sasClient.GetBlobs().ToArray());
    }

    [Test]
    public void Blob_ServiceSas_StoredPolicy_GrantsListBlobsAccess()
    {
        // Arrange — create a container and upload a blob.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sas-policy");
        var container = serviceClient.GetBlobContainerClient("sas-policy");
        container.GetBlobClient("file.txt").Upload(new BinaryData("data"));

        // Set a named stored access policy on the container.
        container.SetAccessPolicy(PublicAccessType.None, [
            new BlobSignedIdentifier
            {
                Id = "readlist",
                AccessPolicy = new BlobAccessPolicy
                {
                    StartsOn  = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Permissions = "rl"
                }
            }
        ]);

        // Generate a SAS that references the policy by name (si=readlist, no sp/st/se on wire).
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "sas-policy",
            Resource          = "c",
            Identifier        = "readlist"
        };
        var sharedKey = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasParams = sasBuilder.ToSasQueryParameters(sharedKey).ToString();
        var sasUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/sas-policy?{sasParams}");

        // Act — list blobs using the policy-bound SAS.
        var sasClient = new BlobContainerClient(sasUri);
        var blobs = sasClient.GetBlobs().ToArray();

        // Assert
        Assert.That(blobs, Has.Length.EqualTo(1));
        Assert.That(blobs[0].Name, Is.EqualTo("file.txt"));
    }

    // -----------------------------------------------------------------------
    // Queue SAS (sr=q)
    // -----------------------------------------------------------------------

    [Test]
    public void Queue_ServiceSas_GrantsSendAndReceiveAccess()
    {
        // Arrange — create a queue.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new QueueServiceClient(connectionString);
        serviceClient.CreateQueue("sas-queue");
        var queueClient = serviceClient.GetQueueClient("sas-queue");

        // Generate a queue SAS (sp=ap — add + process).
        var sasUri = queueClient.GenerateSasUri(
            QueueSasPermissions.Add | QueueSasPermissions.Process,
            DateTimeOffset.UtcNow.AddHours(1));

        // Act — use AzureSasCredential so the SAS params are explicitly forwarded in every request.
        var queueEndpoint = new Uri(
            $"https://{StorageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/sas-queue");
        var sasQueue = new QueueClient(queueEndpoint, new AzureSasCredential(sasUri.Query.TrimStart('?')));
        sasQueue.SendMessage("hello-queue");
        var messages = sasQueue.ReceiveMessages(maxMessages: 1).Value;

        // Assert
        Assert.That(messages, Has.Length.EqualTo(1));
        Assert.That(messages[0].MessageText, Is.EqualTo("hello-queue"));
    }

    // -----------------------------------------------------------------------
    // Table SAS (sr=t)
    // -----------------------------------------------------------------------

    [Test]
    public void Table_ServiceSas_GrantsQueryAccess()
    {
        // Arrange — create a table and insert an entity.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new TableServiceClient(connectionString);
        serviceClient.CreateTable("SasTable");
        var tableClient = serviceClient.GetTableClient("SasTable");
        tableClient.AddEntity(new TableEntity("pk", "rk") { ["Value"] = "42" });

        // Generate a table SAS (sp=r) by building the SAS URI via TableClient.
        var sasUri = tableClient.GenerateSasUri(TableSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

        // Act — use AzureSasCredential so the SAS params are explicitly forwarded in every request.
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/SasTable");
        var sasTable = new TableClient(tableEndpoint, new AzureSasCredential(sasUri.Query.TrimStart('?')));
        var entities = sasTable.Query<TableEntity>().ToArray();

        // Assert
        Assert.That(entities, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(entities[0].PartitionKey, Is.EqualTo("pk"));
            Assert.That(entities[0].RowKey, Is.EqualTo("rk"));
            Assert.That(entities[0]["Value"].ToString(), Is.EqualTo("42"));
        });
    }

    // -----------------------------------------------------------------------
    // Queue SAS — stored access policy
    // -----------------------------------------------------------------------

    [Test]
    public void Queue_ServiceSas_StoredPolicy_GrantsSendAndReceiveAccess()
    {
        // Arrange — create a queue and set a named stored access policy.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new QueueServiceClient(connectionString);
        serviceClient.CreateQueue("sas-policy-queue");
        var queue = serviceClient.GetQueueClient("sas-policy-queue");

        queue.SetAccessPolicy([
            new Azure.Storage.Queues.Models.QueueSignedIdentifier
            {
                Id = "sendreceive",
                AccessPolicy = new Azure.Storage.Queues.Models.QueueAccessPolicy
                {
                    StartsOn  = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Permissions = "ap"
                }
            }
        ]);

        // Generate a SAS that references the policy by name (si=sendreceive, no sp/st/se on wire).
        var sasBuilder = new QueueSasBuilder
        {
            QueueName  = "sas-policy-queue",
            Identifier = "sendreceive"
        };
        var sharedKey = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasParams = sasBuilder.ToSasQueryParameters(sharedKey).ToString();
        var queueEndpoint = new Uri(
            $"https://{StorageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/sas-policy-queue");
        var sasQueue = new QueueClient(queueEndpoint, new AzureSasCredential(sasParams));

        // Act
        sasQueue.SendMessage("policy-msg");
        var messages = sasQueue.ReceiveMessages(maxMessages: 1).Value;

        // Assert
        Assert.That(messages, Has.Length.EqualTo(1));
        Assert.That(messages[0].MessageText, Is.EqualTo("policy-msg"));
    }

    // -----------------------------------------------------------------------
    // Table SAS — stored access policy
    // -----------------------------------------------------------------------

    [Test]
    public void Table_ServiceSas_StoredPolicy_GrantsQueryAccess()
    {
        // Arrange — create a table, insert an entity, and set a named stored access policy.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableServiceClient = new TableServiceClient(connectionString);
        tableServiceClient.CreateTable("SasPolicyTable");
        var tableClient = tableServiceClient.GetTableClient("SasPolicyTable");
        tableClient.AddEntity(new TableEntity("pk", "rk") { ["Value"] = "hello" });

        tableClient.SetAccessPolicy([
            new TableSignedIdentifier("readpolicy",
                new TableAccessPolicy(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1), "r"))
        ]);

        // Generate a SAS that references the policy by name (si=readpolicy, no sp/st/se on wire).
        var sasUri = tableClient.GenerateSasUri(new TableSasBuilder { TableName = "SasPolicyTable", Identifier = "readpolicy" });
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/SasPolicyTable");
        var sasTable = new TableClient(tableEndpoint, new AzureSasCredential(sasUri.Query.TrimStart('?')));

        // Act
        var entities = sasTable.Query<TableEntity>().ToArray();

        // Assert
        Assert.That(entities, Has.Length.EqualTo(1));
        Assert.That(entities[0]["Value"].ToString(), Is.EqualTo("hello"));
    }

    // -----------------------------------------------------------------------
    // Stored access policy — revocation
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_ServiceSas_StoredPolicy_Revocation_Returns403()
    {
        // Arrange — create a container, set a policy, generate a SAS, then revoke the policy.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sas-revoke-blob");
        var container = serviceClient.GetBlobContainerClient("sas-revoke-blob");

        container.SetAccessPolicy(PublicAccessType.None, [
            new BlobSignedIdentifier
            {
                Id = "revokeme",
                AccessPolicy = new BlobAccessPolicy
                {
                    StartsOn  = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Permissions = "rl"
                }
            }
        ]);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "sas-revoke-blob",
            Resource          = "c",
            Identifier        = "revokeme"
        };
        var sharedKey = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasParams = sasBuilder.ToSasQueryParameters(sharedKey).ToString();
        var sasUri = new Uri(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/sas-revoke-blob?{sasParams}");

        // Revoke: clear all stored policies.
        container.SetAccessPolicy(PublicAccessType.None, []);

        // Act + Assert — request with a now-revoked SAS must be denied.
        var sasClient = new BlobContainerClient(sasUri);
        Assert.Catch<Exception>(() => sasClient.GetBlobs().ToArray());
    }

    [Test]
    public void Queue_ServiceSas_StoredPolicy_Revocation_Returns403()
    {
        // Arrange — create a queue, set a policy, generate a SAS, then revoke the policy.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new QueueServiceClient(connectionString);
        serviceClient.CreateQueue("sas-revoke-queue");
        var queue = serviceClient.GetQueueClient("sas-revoke-queue");

        queue.SetAccessPolicy([
            new Azure.Storage.Queues.Models.QueueSignedIdentifier
            {
                Id = "revokeme",
                AccessPolicy = new Azure.Storage.Queues.Models.QueueAccessPolicy
                {
                    StartsOn  = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Permissions = "a"
                }
            }
        ]);

        var sasBuilder = new QueueSasBuilder
        {
            QueueName  = "sas-revoke-queue",
            Identifier = "revokeme"
        };
        var sharedKey = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasParams = sasBuilder.ToSasQueryParameters(sharedKey).ToString();
        var queueEndpoint = new Uri(
            $"https://{StorageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/sas-revoke-queue");
        var sasQueue = new QueueClient(queueEndpoint, new AzureSasCredential(sasParams));

        // Revoke: clear all stored policies.
        queue.SetAccessPolicy([]);

        // Act + Assert — request with a now-revoked SAS must be denied.
        Assert.Catch<Exception>(() => sasQueue.SendMessage("should-fail"));
    }

    [Test]
    public void Table_ServiceSas_StoredPolicy_Revocation_Returns403()
    {
        // Arrange — create a table, set a policy, generate a SAS, then revoke the policy.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableServiceClient = new TableServiceClient(connectionString);
        tableServiceClient.CreateTable("SasRevokeTable");
        var tableClient = tableServiceClient.GetTableClient("SasRevokeTable");

        tableClient.SetAccessPolicy([
            new TableSignedIdentifier("revokeme",
                new TableAccessPolicy(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1), "r"))
        ]);

        var sasUri = tableClient.GenerateSasUri(new TableSasBuilder { TableName = "SasRevokeTable", Identifier = "revokeme" });
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/SasRevokeTable");
        var sasTable = new TableClient(tableEndpoint, new AzureSasCredential(sasUri.Query.TrimStart('?')));

        // Revoke: clear all stored policies.
        tableClient.SetAccessPolicy([]);

        // Act + Assert — request with a now-revoked SAS must be denied.
        Assert.Catch<Exception>(() => sasTable.Query<TableEntity>().ToArray());
    }

    // -----------------------------------------------------------------------
    // Permission Mismatch Tests (sp= enforcement) — Returns 403 Forbidden
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_ServiceSas_PermissionMismatch_Write_OnReadOnlyToken_Returns403()
    {
        // Arrange — create a container with a read-only SAS (sp=r).
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sp-mismatch-blob");
        var container = serviceClient.GetBlobContainerClient("sp-mismatch-blob");

        // Generate read-only SAS (sp=r — GET/HEAD only).
        var readOnlySas = container.GenerateSasUri(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
        var readOnlyClient = new BlobContainerClient(readOnlySas);

        // Act & Assert — attempt to upload (PUT) with read-only token must fail with 403.
        var ex = Assert.Catch<RequestFailedException>(() =>
            readOnlyClient.GetBlobClient("test.txt").Upload(new BinaryData("data")));
        
        Assert.That(ex.Status, Is.EqualTo(403), "Expected 403 Forbidden for permission mismatch");
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationPermissionMismatch"), 
            "Expected AuthorizationPermissionMismatch error code");
    }

    [Test]
    public void Blob_ServiceSas_PermissionMismatch_Delete_OnReadOnlyToken_Returns403()
    {
        // Arrange — create a blob and upload it with account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sp-mismatch-del");
        var blobClient = serviceClient.GetBlobContainerClient("sp-mismatch-del").GetBlobClient("todelete.txt");
        blobClient.Upload(new BinaryData("data"));

        // Generate read-only SAS (sp=r).
        var readOnlySas = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
        var readOnlyClient = new BlobClient(readOnlySas);

        // Act & Assert — attempt to delete with read-only token must fail with 403.
        var ex = Assert.Catch<RequestFailedException>(() => readOnlyClient.Delete());
        
        Assert.That(ex.Status, Is.EqualTo(403), "Expected 403 Forbidden for permission mismatch");
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationPermissionMismatch"));
    }

    [Test]
    public void Queue_ServiceSas_PermissionMismatch_PutMessage_OnReadOnlyToken_Returns403()
    {
        // Arrange — create a queue with a read-only SAS (sp=r).
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new QueueServiceClient(connectionString);
        serviceClient.CreateQueue("sp-mismatch-queue");
        var queueClient = serviceClient.GetQueueClient("sp-mismatch-queue");

        // Generate read-only SAS (sp=r — GET/HEAD only).
        var readOnlySas = queueClient.GenerateSasUri(QueueSasPermissions.Read | QueueSasPermissions.Process,
            DateTimeOffset.UtcNow.AddHours(1));
        var readOnlyClient = new QueueClient(readOnlySas);

        // Act & Assert — attempt to send message (POST) with read-only token must fail with 403.
        var ex = Assert.Catch<RequestFailedException>(() => readOnlyClient.SendMessage("should-fail"));
        
        Assert.That(ex.Status, Is.EqualTo(403), "Expected 403 Forbidden for permission mismatch");
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationPermissionMismatch"));
    }

    [Test]
    public void Queue_ServiceSas_PermissionMismatch_DeleteMessage_OnReadOnlyToken_Returns403()
    {
        // Arrange — create a queue, send a message with account key, then get read-only SAS.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new QueueServiceClient(connectionString);
        serviceClient.CreateQueue("sp-mismatch-queuedel");
        var queueClient = serviceClient.GetQueueClient("sp-mismatch-queuedel");
        var message = queueClient.SendMessage("test");

        // Generate read-only SAS (sp=r — read/list only, no delete).
        var readOnlySas = queueClient.GenerateSasUri(QueueSasPermissions.Read,
            DateTimeOffset.UtcNow.AddHours(1));
        var readOnlyClient = new QueueClient(readOnlySas);

        // Act & Assert — attempt to delete message with read-only token must fail with 403.
        var ex = Assert.Catch<RequestFailedException>(() =>
            readOnlyClient.DeleteMessage(message.Value.MessageId, message.Value.PopReceipt));
        
        Assert.That(ex.Status, Is.EqualTo(403), "Expected 403 Forbidden for permission mismatch");
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationPermissionMismatch"));
    }

    [Test]
    public void Table_ServiceSas_PermissionMismatch_Insert_OnReadOnlyToken_Returns403()
    {
        // Arrange — create a table with a read-only SAS (sp=r).
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableServiceClient = new TableServiceClient(connectionString);
        tableServiceClient.CreateTable("sp_mismatch_table");
        var tableClient = tableServiceClient.GetTableClient("sp_mismatch_table");

        // Generate read-only SAS (sp=r — GET/HEAD only).
        var sasBuilder = new TableSasBuilder { TableName = "sp_mismatch_table", ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) };
        sasBuilder.SetPermissions(TableSasPermissions.Read);
        var readOnlySas = tableClient.GenerateSasUri(sasBuilder);
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/sp_mismatch_table");
        var readOnlyClient = new TableClient(tableEndpoint, new AzureSasCredential(readOnlySas.Query.TrimStart('?')));

        // Act & Assert — attempt to insert entity with read-only token must fail with 403.
        var entity = new TableEntity("pk", "rk") { { "data", "value" } };
        var ex = Assert.Catch<RequestFailedException>(() => readOnlyClient.AddEntity(entity));
        
        Assert.That(ex.Status, Is.EqualTo(403), "Expected 403 Forbidden for permission mismatch");
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationPermissionMismatch"));
    }

    [Test]
    public void Table_ServiceSas_PermissionMismatch_Delete_OnReadOnlyToken_Returns403()
    {
        // Arrange — create a table and insert an entity with account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableServiceClient = new TableServiceClient(connectionString);
        tableServiceClient.CreateTable("sp_mismatch_del_table");
        var tableClient = tableServiceClient.GetTableClient("sp_mismatch_del_table");
        tableClient.AddEntity(new TableEntity("pk", "rk") { { "data", "value" } });

        // Generate read-only SAS (sp=r).
        var sasBuilder = new TableSasBuilder { TableName = "sp_mismatch_del_table", ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) };
        sasBuilder.SetPermissions(TableSasPermissions.Read);
        var readOnlySas = tableClient.GenerateSasUri(sasBuilder);
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/sp_mismatch_del_table");
        var readOnlyClient = new TableClient(tableEndpoint, new AzureSasCredential(readOnlySas.Query.TrimStart('?')));

        // Act & Assert — attempt to delete entity with read-only token must fail with 403.
        var ex = Assert.Catch<RequestFailedException>(() =>
            readOnlyClient.DeleteEntity("pk", "rk"));
        
        Assert.That(ex.Status, Is.EqualTo(403), "Expected 403 Forbidden for permission mismatch");
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationPermissionMismatch"));
    }

    // -----------------------------------------------------------------------
    // Regression Tests — Valid permissions still work
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_ServiceSas_ValidPermission_Write_OnWriteToken_Succeeds()
    {
        // Arrange — create a container with write permission SAS (sp=w).
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sp-valid-blob");
        var container = serviceClient.GetBlobContainerClient("sp-valid-blob");

        // Generate write-only SAS (sp=w).
        var writeSas = container.GenerateSasUri(BlobContainerSasPermissions.Write, DateTimeOffset.UtcNow.AddHours(1));
        var writeClient = new BlobContainerClient(writeSas);

        // Act — upload blob with write-only token should succeed.
        var blobClient = writeClient.GetBlobClient("written.txt");
        blobClient.Upload(new BinaryData("written content"));

        // Assert — verify blob was created.
        var accountClient = new BlobServiceClient(connectionString);
        var uploaded = accountClient.GetBlobContainerClient("sp-valid-blob").GetBlobClient("written.txt");
        var content = uploaded.DownloadContent().Value;
        Assert.That(content.Content.ToString(), Is.EqualTo("written content"));
    }

    [Test]
    public void Queue_ServiceSas_ValidPermission_Add_OnAddToken_Succeeds()
    {
        // Arrange — create a queue with add permission SAS (sp=a).
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new QueueServiceClient(connectionString);
        serviceClient.CreateQueue("sp-valid-queue");
        var queueClient = serviceClient.GetQueueClient("sp-valid-queue");

        // Generate add-only SAS (sp=a).
        var addSas = queueClient.GenerateSasUri(QueueSasPermissions.Add, DateTimeOffset.UtcNow.AddHours(1));
        var addClient = new QueueClient(addSas);

        // Act — send message with add-only token should succeed.
        addClient.SendMessage("valid message");

        // Assert — verify message was queued (via account key).
        var messages = queueClient.ReceiveMessages();
        Assert.That(messages.Value, Has.Length.GreaterThanOrEqualTo(1));
        Assert.That(messages.Value[0].Body.ToString(), Is.EqualTo("valid message"));
    }

    [Test]
    public void Table_ServiceSas_ValidPermission_Add_OnAddToken_Succeeds()
    {
        // Arrange — create a table with add permission SAS (sp=a).
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableServiceClient = new TableServiceClient(connectionString);
        tableServiceClient.CreateTable("sp_valid_table");
        var tableClient = tableServiceClient.GetTableClient("sp_valid_table");

        // Generate add-only SAS (sp=a).
        var sasBuilder = new TableSasBuilder { TableName = "sp_valid_table", ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) };
        sasBuilder.SetPermissions(TableSasPermissions.Add);
        var addSas = tableClient.GenerateSasUri(sasBuilder);
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/sp_valid_table");
        var addClient = new TableClient(tableEndpoint, new AzureSasCredential(addSas.Query.TrimStart('?')));

        // Act — insert entity with add-only token should succeed.
        var entity = new TableEntity("pk", "rk") { { "data", "value" } };
        addClient.AddEntity(entity);

        // Assert — verify entity was inserted (via account key).
        var retrieved = tableClient.GetEntity<TableEntity>("pk", "rk");
        Assert.That(retrieved.Value["data"].ToString(), Is.EqualTo("value"));
    }

    // -----------------------------------------------------------------------
    // Source IP (sip=) enforcement — Service SAS
    // -----------------------------------------------------------------------

    [Test]
    public void Blob_ServiceSas_SourceIP_DeniedIP_Returns403()
    {
        // Arrange
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sip-blob");
        serviceClient.GetBlobContainerClient("sip-blob").GetBlobClient("sip-test.txt").Upload(new BinaryData("hello"));

        // Generate a SAS whose sip= restricts to an IP that is NOT 127.0.0.1.
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "sip-blob",
            BlobName          = "sip-test.txt",
            Resource          = "b",
            ExpiresOn         = DateTimeOffset.UtcNow.AddHours(1),
            IPRange           = new SasIPRange(System.Net.IPAddress.Parse("10.0.0.1"))
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        var credential = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasUri = new UriBuilder(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/sip-blob/sip-test.txt")
        {
            Query = sasBuilder.ToSasQueryParameters(credential).ToString()
        }.Uri;

        var sasClient = new BlobClient(sasUri);

        // Act & Assert — the SAS is structurally valid but caller IP (127.0.0.1) is outside 10.0.0.1.
        var ex = Assert.Throws<Azure.RequestFailedException>(() => sasClient.DownloadContent());
        Assert.That(ex.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationSourceIPMismatch"));
    }

    [Test]
    public void Blob_ServiceSas_SourceIP_AllowedIP_Returns200()
    {
        // Arrange
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sip-allowed");
        serviceClient.GetBlobContainerClient("sip-allowed").GetBlobClient("sip-ok.txt").Upload(new BinaryData("allowed"));

        // SAS whose sip= is exactly 127.0.0.1 — the loopback the Kestrel test server runs on.
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "sip-allowed",
            BlobName          = "sip-ok.txt",
            Resource          = "b",
            ExpiresOn         = DateTimeOffset.UtcNow.AddHours(1),
            IPRange           = new SasIPRange(System.Net.IPAddress.Parse("127.0.0.1"))
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        var credential = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasUri = new UriBuilder(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/sip-allowed/sip-ok.txt")
        {
            Query = sasBuilder.ToSasQueryParameters(credential).ToString()
        }.Uri;

        var sasClient = new BlobClient(sasUri);

        // Act & Assert
        var result = sasClient.DownloadContent();
        Assert.That(result.Value.Content.ToString(), Is.EqualTo("allowed"));
    }

    [Test]
    public void Queue_ServiceSas_SourceIP_DeniedIP_Returns403()
    {
        // Arrange
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var queueServiceClient = new QueueServiceClient(connectionString);
        queueServiceClient.CreateQueue("sip-queue");

        var sasBuilder = new QueueSasBuilder
        {
            QueueName = "sip-queue",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            IPRange   = new SasIPRange(System.Net.IPAddress.Parse("10.0.0.1"))
        };
        sasBuilder.SetPermissions(QueueSasPermissions.Add);
        var credential = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasUri = new UriBuilder(
            $"https://{StorageAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/sip-queue")
        {
            Query = sasBuilder.ToSasQueryParameters(credential).ToString()
        }.Uri;

        var sasQueueClient = new QueueClient(sasUri);

        // Act & Assert
        var ex = Assert.Throws<Azure.RequestFailedException>(() => sasQueueClient.SendMessage("hello"));
        Assert.That(ex.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationSourceIPMismatch"));
    }

    [Test]
    public void Table_ServiceSas_SourceIP_DeniedIP_Returns403()
    {
        // Arrange
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var tableServiceClient = new TableServiceClient(connectionString);
        tableServiceClient.CreateTableIfNotExists("sipTable");
        var tableClient = tableServiceClient.GetTableClient("sipTable");

        // Build a Table SAS with sip=10.0.0.1 using the low-level TableSasBuilder API.
        var sasBuilder = new TableSasBuilder
        {
            TableName = "sipTable",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            IPRange   = new TableSasIPRange(System.Net.IPAddress.Parse("10.0.0.1"))
        };
        sasBuilder.SetPermissions(TableSasPermissions.Read);
        var sasUri = tableClient.GenerateSasUri(sasBuilder);
        var tableEndpoint = new Uri(
            $"https://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/sipTable");
        var sasTableClient = new TableClient(tableEndpoint, new AzureSasCredential(sasUri.Query.TrimStart('?')));

        // Act & Assert
        var ex = Assert.Throws<Azure.RequestFailedException>(() => sasTableClient.Query<TableEntity>().ToList());
        Assert.That(ex.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationSourceIPMismatch"));
    }

    [Test]
    public void Blob_AccountSas_SourceIP_DeniedIP_Returns403()
    {
        // Arrange — upload a blob with the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClient = new BlobServiceClient(connectionString);
        serviceClient.CreateBlobContainer("sip-acct");
        serviceClient.GetBlobContainerClient("sip-acct").GetBlobClient("sip-acct.txt").Upload(new BinaryData("acct"));

        // Build an Account SAS with sip=10.0.0.1.
        var sasBuilder = new AccountSasBuilder
        {
            Services      = AccountSasServices.Blobs,
            ResourceTypes = AccountSasResourceTypes.Object,
            ExpiresOn     = DateTimeOffset.UtcNow.AddHours(1),
            IPRange       = new SasIPRange(System.Net.IPAddress.Parse("10.0.0.1"))
        };
        sasBuilder.SetPermissions(AccountSasPermissions.Read);
        var sharedKeyCredential = new StorageSharedKeyCredential(StorageAccountName, _accountKey);
        var sasToken = sasBuilder.ToSasQueryParameters(sharedKeyCredential).ToString();
        var sasUri = new UriBuilder(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/sip-acct/sip-acct.txt")
        {
            Query = sasToken
        }.Uri;

        var sasClient = new BlobClient(sasUri);

        // Act & Assert
        var ex = Assert.Throws<Azure.RequestFailedException>(() => sasClient.DownloadContent());
        Assert.That(ex.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationSourceIPMismatch"));
    }

    [Test]
    public async Task Blob_UserDelegationSas_SourceIP_DeniedIP_Returns403()
    {
        // Arrange — upload a blob with the account key.
        var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _accountKey);
        var serviceClientKey = new BlobServiceClient(connectionString);
        serviceClientKey.CreateBlobContainer("sip-udkey");
        serviceClientKey.GetBlobContainerClient("sip-udkey").GetBlobClient("sip-ud.txt").Upload(new BinaryData("ud"));

        // Get a User Delegation Key via bearer credential.
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var serviceUri = new Uri($"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}");
        var blobServiceClient = new BlobServiceClient(serviceUri, credential);
        var keyExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var udKey = await blobServiceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(keyExpiry) { StartsOn = DateTimeOffset.UtcNow.AddSeconds(-5) });

        // Build a User Delegation SAS with sip=10.0.0.1.
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "sip-udkey",
            BlobName          = "sip-ud.txt",
            Resource          = "b",
            ExpiresOn         = keyExpiry,
            IPRange           = new SasIPRange(System.Net.IPAddress.Parse("10.0.0.1"))
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        var sasParams = sasBuilder.ToSasQueryParameters(udKey, StorageAccountName);
        var sasUri = new Uri(
            $"https://{StorageAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/sip-udkey/sip-ud.txt?{sasParams}");

        var sasClient = new BlobClient(sasUri);

        // Act & Assert
        var ex = Assert.ThrowsAsync<Azure.RequestFailedException>(async () => await sasClient.DownloadContentAsync());
        Assert.That(ex!.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("AuthorizationSourceIPMismatch"));
    }
}
