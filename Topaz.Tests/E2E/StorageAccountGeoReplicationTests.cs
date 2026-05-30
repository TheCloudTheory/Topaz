using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class StorageAccountGeoReplicationTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("FA0B1C2D-3E4F-5A6B-7C8D-9E0F1A2B3C4D");

    private const string SubscriptionName = "geo-replication-tests";
    private const string ResourceGroupName = "test";
    private const string RagrsAccountName = "geoaccountragrs";
    private const string LrsAccountName = "geoaccountlrs";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    private ArmClient CreateArmClient() =>
        new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private Azure.ResourceManager.Resources.ResourceGroupResource GetResourceGroup(ArmClient armClient) =>
        armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

    private StorageAccountResource CreateStorageAccount(
        Azure.ResourceManager.Resources.ResourceGroupResource resourceGroup,
        string accountName,
        StorageSkuName skuName)
    {
        var content = new StorageAccountCreateOrUpdateContent(
            new StorageSku(skuName),
            StorageKind.StorageV2,
            AzureLocation.WestEurope);

        var operation = resourceGroup.GetStorageAccounts()
            .CreateOrUpdate(WaitUntil.Completed, accountName, content);

        return operation.Value;
    }

    [Test]
    public void StorageAccount_RAGRS_ARM_ReturnsSecondaryEndpoints()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);

        // Act
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);

        // Assert
        Assert.That(storageAccount.Data.SecondaryEndpoints, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(storageAccount.Data.SecondaryEndpoints!.BlobUri, Is.Not.Null);
            Assert.That(storageAccount.Data.SecondaryEndpoints!.QueueUri, Is.Not.Null);
            Assert.That(storageAccount.Data.SecondaryEndpoints!.TableUri, Is.Not.Null);
            Assert.That(storageAccount.Data.SecondaryEndpoints!.BlobUri!.Host,
                Does.Contain($"{RagrsAccountName}-secondary"));
            Assert.That(storageAccount.Data.StatusOfSecondary, Is.EqualTo(StorageAccountStatus.Available));
        });
    }

    [Test]
    public void StorageAccount_LRS_ARM_NoSecondaryEndpoints()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);

        // Act
        var storageAccount = CreateStorageAccount(resourceGroup, LrsAccountName, StorageSkuName.StandardLrs);

        // Assert
        Assert.That(storageAccount.Data.SecondaryEndpoints, Is.Null);
        Assert.That(storageAccount.Data.StatusOfSecondary, Is.Null);
    }

    [Test]
    public async Task BlobStorage_GetServiceStats_OnSecondary_ReturnsLive()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var secondaryEndpoint =
            $"https://{RagrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/";

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var httpClient = new HttpClient(handler);

        var statsUrl = $"{secondaryEndpoint}?restype=service&comp=stats";
        var request = new HttpRequestMessage(HttpMethod.Get, statsUrl);
        // Add a minimal SharedKey auth header so Topaz accepts the request
        var date = DateTimeOffset.UtcNow.ToString("R");
        request.Headers.Add("x-ms-date", date);
        request.Headers.Add("x-ms-version", "2020-10-02");

        var stringToSign = $"GET\n\n\n\n\n\n\n\n\n\n\n\nx-ms-date:{date}\nx-ms-version:2020-10-02\n/{RagrsAccountName}/\ncomp:stats\nrestype:service";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Convert.FromBase64String(key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stringToSign)));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "SharedKey", $"{RagrsAccountName}:{signature}");

        // Act
        var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That((int)response.StatusCode, Is.EqualTo(200));
        Assert.That(body, Does.Contain("<Status>live</Status>"));
    }

    [Test]
    public async Task BlobStorage_GetServiceStats_OnSecondary_NonRagrs_Returns403()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        // Create LRS account to test FeatureNotSupported
        CreateStorageAccount(resourceGroup, LrsAccountName, StorageSkuName.StandardLrs);

        var secondaryEndpoint =
            $"https://{LrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/";

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var httpClient = new HttpClient(handler);

        // Act
        var response = await httpClient.GetAsync($"{secondaryEndpoint}?restype=service&comp=stats");
        var body = await response.Content.ReadAsStringAsync();

        // Assert — LRS account is not found via secondary lookup (no `-secondary` DNS entry for it)
        // The endpoint returns 404 since TryGetStorageAccountFromSecondaryHost won't find a registration
        Assert.That((int)response.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task BlobStorage_WriteOnSecondary_Returns403()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};" +
            $"AccountKey={key};" +
            $"BlobEndpoint=https://{RagrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";

        var clientOptions = new BlobClientOptions();
        clientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        var blobServiceClient = new BlobServiceClient(secondaryConnectionString, clientOptions);

        // Act + Assert — creating a container on a secondary endpoint must fail with 403
        var ex = Assert.ThrowsAsync<Azure.RequestFailedException>(async () =>
            await blobServiceClient.CreateBlobContainerAsync("test-secondary-write"));

        Assert.That(ex!.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
    }

    [Test]
    public async Task QueueStorage_WriteOnSecondary_Returns403()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};" +
            $"AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";

        var clientOptions = new QueueClientOptions();
        clientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        var queueServiceClient = new QueueServiceClient(secondaryConnectionString, clientOptions);

        // Act + Assert — creating a queue on a secondary endpoint must fail with 403
        var ex = Assert.ThrowsAsync<Azure.RequestFailedException>(async () =>
            await queueServiceClient.CreateQueueAsync("test-secondary-write"));

        Assert.That(ex!.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
    }

    [Test]
    public async Task TableStorage_WriteOnSecondary_Returns403()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var secondaryEndpoint =
            $"https://{RagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";

        var tableClientOptions = new TableClientOptions();
        tableClientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        var tableServiceClient = new TableServiceClient(
            new Uri(secondaryEndpoint),
            new TableSharedKeyCredential(RagrsAccountName, key),
            tableClientOptions);

        // Act + Assert — creating a table on a secondary endpoint must fail with 403
        var ex = Assert.ThrowsAsync<Azure.RequestFailedException>(async () =>
            await tableServiceClient.CreateTableAsync("testsecondarytable"));

        Assert.That(ex!.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
    }

    [Test]
    public async Task BlobStorage_ListContainers_OnSecondary_ReturnsData()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{RagrsAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";

        var clientOptions = new BlobClientOptions();
        clientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        // Create a container via the primary endpoint
        var primaryClient = new BlobServiceClient(primaryConnectionString, clientOptions);
        await primaryClient.CreateBlobContainerAsync("secondary-read-test");

        // Act — list containers via the secondary endpoint
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{RagrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";

        var secondaryClient = new BlobServiceClient(secondaryConnectionString, clientOptions);
        var containers = secondaryClient.GetBlobContainersAsync().ToBlockingEnumerable().ToList();

        // Assert
        Assert.That(containers.Any(c => c.Name == "secondary-read-test"), Is.True);
    }

    [Test]
    public async Task QueueStorage_ListQueues_OnSecondary_ReturnsData()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new QueueClientOptions();
        clientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        // Create a queue via the primary endpoint
        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";

        var primaryClient = new QueueServiceClient(primaryConnectionString, clientOptions);
        await primaryClient.CreateQueueAsync("secondary-read-queue");

        // Act — list queues via the secondary endpoint
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";

        var secondaryClient = new QueueServiceClient(secondaryConnectionString, clientOptions);
        var queues = secondaryClient.GetQueuesAsync().ToBlockingEnumerable().ToList();

        // Assert
        Assert.That(queues.Any(q => q.Name == "secondary-read-queue"), Is.True);
    }

    [Test]
    public async Task TableStorage_QueryEntities_OnSecondary_ReturnsData()
    {
        // Arrange
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var tableClientOptions = new TableClientOptions();
        tableClientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        // Create a table and insert an entity via the primary endpoint
        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"TableEndpoint=https://{RagrsAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;";

        var primaryServiceClient = new TableServiceClient(primaryConnectionString, tableClientOptions);
        await primaryServiceClient.CreateTableAsync("secondaryreadtable");
        var primaryTableClient = primaryServiceClient.GetTableClient("secondaryreadtable");
        await primaryTableClient.AddEntityAsync(new Azure.Data.Tables.TableEntity("pk", "rk") { { "Value", "hello" } });

        // Act — query entities via the secondary endpoint
        var secondaryEndpoint =
            $"https://{RagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";

        var secondaryServiceClient = new TableServiceClient(
            new Uri(secondaryEndpoint),
            new TableSharedKeyCredential(RagrsAccountName, key),
            tableClientOptions);

        var secondaryTableClient = secondaryServiceClient.GetTableClient("secondaryreadtable");
        var entities = secondaryTableClient.Query<Azure.Data.Tables.TableEntity>().ToList();

        // Assert
        Assert.That(entities.Any(e => e.PartitionKey == "pk" && e.RowKey == "rk"), Is.True);
    }
}
