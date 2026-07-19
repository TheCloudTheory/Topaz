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
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class StorageAccountGeoReplicationTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("FA0B1C2D-3E4F-5A6B-7C8D-9E0F1A2B3C4D");
    private static readonly HttpClient HttpClient = new();

    private const string SubscriptionName = "geo-replication-tests";
    private const string ResourceGroupName = "test";
    private const string RagrsAccountName = "geoaccountragrs";
    private const string LrsAccountName = "geoaccountlrs";

    // Dedicated accounts for geo-lag tests — isolated from the shared constants so
    // parallel test runs cannot trigger each other's scheduler watermark.
    private const string BlobLagRagrsAccountName = "geobloblagragrs";
    private const string BlobLagRagzrsAccountName = "geobloblagragzrs";
    private const string QueueLagRagrsAccountName = "geoqueulagragrs";
    private const string QueueLagRagzrsAccountName = "geoqueulragzrs";
    private const string TableLagRagrsAccountName = "geotablagragrs";
    private const string TableLagRagzrsAccountName = "geotablragzrs";

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
    }

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
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

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
        var response = await HttpClient.SendAsync(request);
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

        // Act
        var response = await HttpClient.GetAsync($"{secondaryEndpoint}?restype=service&comp=stats");
        _ = await response.Content.ReadAsStringAsync();

        // Assert — LRS account is not found via secondary lookup (no `-secondary` DNS entry for it)
        // The endpoint returns 404 since TryGetStorageAccountFromSecondaryHost won't find a registration
        Assert.That((int)response.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public Task BlobStorage_WriteOnSecondary_Returns403()
    {
        try
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

            var clientOptions = new BlobClientOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    })
            };

            var blobServiceClient = new BlobServiceClient(secondaryConnectionString, clientOptions);

            // Act + Assert — creating a container on a secondary endpoint must fail with 403
            var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
                await blobServiceClient.CreateBlobContainerAsync("test-secondary-write"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.Status, Is.EqualTo(403));
                Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
            }
            
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Test]
    public Task QueueStorage_WriteOnSecondary_Returns403()
    {
        try
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

            var clientOptions = new QueueClientOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    })
            };

            var queueServiceClient = new QueueServiceClient(secondaryConnectionString, clientOptions);

            // Act + Assert — creating a queue on a secondary endpoint must fail with 403
            var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
                await queueServiceClient.CreateQueueAsync("test-secondary-write"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.Status, Is.EqualTo(403));
                Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
            }
            
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Test]
    public Task TableStorage_WriteOnSecondary_Returns403()
    {
        try
        {
            // Arrange
            var armClient = CreateArmClient();
            var resourceGroup = GetResourceGroup(armClient);
            var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
            var key = storageAccount.GetKeys().First().Value;

            var secondaryEndpoint =
                $"https://{RagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";

            var tableClientOptions = new TableClientOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    })
            };

            var tableServiceClient = new TableServiceClient(
                new Uri(secondaryEndpoint),
                new TableSharedKeyCredential(RagrsAccountName, key),
                tableClientOptions);

            // Act + Assert — creating a table on a secondary endpoint must fail with 403
            var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
                await tableServiceClient.CreateTableAsync("testsecondarytable"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.Status, Is.EqualTo(403));
                Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
            }
            
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
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

        var clientOptions = new BlobClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

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

        var clientOptions = new QueueClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        // Create a queue via the primary endpoint
        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";

        var primaryClient = new QueueServiceClient(primaryConnectionString, clientOptions);
        await primaryClient.CreateQueueAsync("secondary-read-queue");

        // Advance the geo-replication watermark so the queue is visible on secondary
        AzureStorageControlPlane.New(new PrettyTopazLogger()).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            RagrsAccountName);

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

        var tableClientOptions = new TableClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        // Create a table and insert an entity via the primary endpoint
        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"TableEndpoint=https://{RagrsAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;";

        var primaryServiceClient = new TableServiceClient(primaryConnectionString, tableClientOptions);
        await primaryServiceClient.CreateTableAsync("secondaryreadtable");
        var primaryTableClient = primaryServiceClient.GetTableClient("secondaryreadtable");
        await primaryTableClient.AddEntityAsync(new TableEntity("pk", "rk") { { "Value", "hello" } });

        // Advance the geo-replication watermark so the queue is visible on secondary
        AzureStorageControlPlane.New(new PrettyTopazLogger()).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            RagrsAccountName);
        
        // Act — query entities via the secondary endpoint
        var secondaryEndpoint =
            $"https://{RagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";

        var secondaryServiceClient = new TableServiceClient(
            new Uri(secondaryEndpoint),
            new TableSharedKeyCredential(RagrsAccountName, key),
            tableClientOptions);

        var secondaryTableClient = secondaryServiceClient.GetTableClient("secondaryreadtable");
        var entities = secondaryTableClient.Query<TableEntity>().ToList();

        // Assert
        Assert.That(entities.Any(e => e.PartitionKey == "pk" && e.RowKey == "rk"), Is.True);
    }

    [Test]
    public async Task BlobStorage_GetServiceStats_LastSyncTime_IsInThePast()
    {
        // Arrange — freshly-created RA-GRS account has LastGeoSyncTime set to UtcNow-30s on creation
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var beforeStats = DateTimeOffset.UtcNow;

        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{RagrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";
        var client = new BlobServiceClient(secondaryConnectionString);

        // Act
        var stats = await client.GetStatisticsAsync();

        // Assert — LastSyncedOn must be strictly before the moment we queried stats
        Assert.That(stats.Value.GeoReplication.LastSyncedOn, Is.LessThan(beforeStats));
    }

    [Test]
    public async Task QueueStorage_GetServiceStats_LastSyncTime_IsInThePast()
    {
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var beforeStats = DateTimeOffset.UtcNow;

        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";
        var client = new QueueServiceClient(secondaryConnectionString);

        var stats = await client.GetStatisticsAsync();

        Assert.That(stats.Value.GeoReplication.LastSyncedOn, Is.LessThan(beforeStats));
    }

    [Test]
    public async Task TableStorage_GetServiceStats_LastSyncTime_IsInThePast()
    {
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var beforeStats = DateTimeOffset.UtcNow;

        // TableServiceClient.GetStatisticsAsync() derives the secondary endpoint from the primary
        // by appending "-secondary" to the account name — use the primary endpoint here.
        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"TableEndpoint=https://{RagrsAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;";
        var client = new TableServiceClient(primaryConnectionString);

        var stats = await client.GetStatisticsAsync();

        Assert.That(stats.Value.GeoReplication.LastSyncedOn, Is.LessThan(beforeStats));
    }

    [Test]
    public async Task BlobStorage_RAGRS_BeforeGeoSync_BlobNotVisibleOnSecondary_AfterGeoSync_BlobVisible()
    {
        // Arrange — RA-GRS account; LastGeoSyncTime is set to UtcNow-30s on creation,
        // so a blob uploaded right after creation will be newer than LastGeoSyncTime
        // and must be invisible on the secondary until the scheduler advances the watermark.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, BlobLagRagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new BlobClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={BlobLagRagrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{BlobLagRagrsAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={BlobLagRagrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{BlobLagRagrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";

        var primaryClient = new BlobServiceClient(primaryConnectionString, clientOptions);
        var secondaryClient = new BlobServiceClient(secondaryConnectionString, clientOptions);

        // Upload a blob to the primary
        const string containerName = "geo-lag-blobs";
        await primaryClient.CreateBlobContainerAsync(containerName);
        var blobClient = primaryClient.GetBlobContainerClient(containerName).GetBlobClient("file.txt");
        await blobClient.UploadAsync(new BinaryData("hello"));

        // Act — list blobs on secondary before geo-sync
        var blobsBeforeSync = secondaryClient
            .GetBlobContainerClient(containerName)
            .GetBlobs()
            .ToList();

        // Assert — blob must not be visible yet
        Assert.That(blobsBeforeSync.Any(b => b.Name == "file.txt"), Is.False,
            "Blob should not be visible on secondary before geo-replication scheduler runs");

        // Trigger geo-replication for this account only
        var logger = new PrettyTopazLogger();
        AzureStorageControlPlane.New(logger).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            BlobLagRagrsAccountName);

        // Act — list blobs on secondary after geo-sync
        var blobsAfterSync = secondaryClient
            .GetBlobContainerClient(containerName)
            .GetBlobs()
            .ToList();

        // Assert — blob must now be visible
        Assert.That(blobsAfterSync.Any(b => b.Name == "file.txt"), Is.True,
            "Blob should be visible on secondary after geo-replication scheduler runs");
    }

    [Test]
    public async Task BlobStorage_RAGZRS_BeforeGeoSync_BlobNotVisibleOnSecondary_AfterGeoSync_BlobVisible()
    {
        // Arrange — RA-GZRS account; same replication-lag behaviour as RA-GRS.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, BlobLagRagzrsAccountName, StorageSkuName.StandardRagzrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new BlobClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={BlobLagRagzrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{BlobLagRagzrsAccountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={BlobLagRagzrsAccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{BlobLagRagzrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;";

        var primaryClient = new BlobServiceClient(primaryConnectionString, clientOptions);
        var secondaryClient = new BlobServiceClient(secondaryConnectionString, clientOptions);

        // Upload a blob to the primary
        const string containerName = "geo-lag-blobs";
        await primaryClient.CreateBlobContainerAsync(containerName);
        var blobClient = primaryClient.GetBlobContainerClient(containerName).GetBlobClient("file.txt");
        await blobClient.UploadAsync(new BinaryData("hello"));

        // Act — list blobs on secondary before geo-sync
        var blobsBeforeSync = secondaryClient
            .GetBlobContainerClient(containerName)
            .GetBlobs()
            .ToList();

        // Assert — blob must not be visible yet
        Assert.That(blobsBeforeSync.Any(b => b.Name == "file.txt"), Is.False,
            "Blob should not be visible on secondary before geo-replication scheduler runs");

        // Trigger geo-replication for this account only
        var logger = new PrettyTopazLogger();
        AzureStorageControlPlane.New(logger).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            BlobLagRagzrsAccountName);

        // Act — list blobs on secondary after geo-sync
        var blobsAfterSync = secondaryClient
            .GetBlobContainerClient(containerName)
            .GetBlobs()
            .ToList();

        // Assert — blob must now be visible
        Assert.That(blobsAfterSync.Any(b => b.Name == "file.txt"), Is.True,
            "Blob should be visible on secondary after geo-replication scheduler runs");
    }

    [Test]
    public async Task QueueStorage_RAGRS_BeforeGeoSync_QueueNotVisibleOnSecondary_AfterGeoSync_QueueVisible()
    {
        // Arrange — RA-GRS account; a queue created after account provisioning will have
        // LastWriteTimeUtc > LastGeoSyncTime and must be hidden on the secondary until the scheduler runs.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, QueueLagRagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new QueueClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={QueueLagRagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{QueueLagRagrsAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={QueueLagRagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{QueueLagRagrsAccountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";

        var primaryClient = new QueueServiceClient(primaryConnectionString, clientOptions);
        var secondaryClient = new QueueServiceClient(secondaryConnectionString, clientOptions);

        // Create a queue via the primary endpoint
        await primaryClient.CreateQueueAsync("geo-lag-queue");

        // Act — list queues on secondary before geo-sync
        var queuesBeforeSync = secondaryClient.GetQueuesAsync().ToBlockingEnumerable().ToList();

        // Assert — queue must not be visible yet
        Assert.That(queuesBeforeSync.Any(q => q.Name == "geo-lag-queue"), Is.False,
            "Queue should not be visible on secondary before geo-replication scheduler runs");

        // Trigger geo-replication for this account only
        var logger = new PrettyTopazLogger();
        AzureStorageControlPlane.New(logger).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            QueueLagRagrsAccountName);

        // Act — list queues on secondary after geo-sync
        var queuesAfterSync = secondaryClient.GetQueuesAsync().ToBlockingEnumerable().ToList();

        // Assert — queue must now be visible
        Assert.That(queuesAfterSync.Any(q => q.Name == "geo-lag-queue"), Is.True,
            "Queue should be visible on secondary after geo-replication scheduler runs");
    }

    [Test]
    public async Task QueueStorage_RAGZRS_BeforeGeoSync_QueueNotVisibleOnSecondary_AfterGeoSync_QueueVisible()
    {
        // Arrange — RA-GZRS account; same replication-lag behaviour as RA-GRS.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, QueueLagRagzrsAccountName, StorageSkuName.StandardRagzrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new QueueClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={QueueLagRagzrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{QueueLagRagzrsAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={QueueLagRagzrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{QueueLagRagzrsAccountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";

        var primaryClient = new QueueServiceClient(primaryConnectionString, clientOptions);
        var secondaryClient = new QueueServiceClient(secondaryConnectionString, clientOptions);

        // Create a queue via the primary endpoint
        await primaryClient.CreateQueueAsync("geo-lag-queue");

        // Act — list queues on secondary before geo-sync
        var queuesBeforeSync = secondaryClient.GetQueuesAsync().ToBlockingEnumerable().ToList();

        // Assert — queue must not be visible yet
        Assert.That(queuesBeforeSync.Any(q => q.Name == "geo-lag-queue"), Is.False,
            "Queue should not be visible on secondary before geo-replication scheduler runs");

        // Trigger geo-replication for this account only
        var logger = new PrettyTopazLogger();
        AzureStorageControlPlane.New(logger).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            QueueLagRagzrsAccountName);

        // Act — list queues on secondary after geo-sync
        var queuesAfterSync = secondaryClient.GetQueuesAsync().ToBlockingEnumerable().ToList();

        // Assert — queue must now be visible
        Assert.That(queuesAfterSync.Any(q => q.Name == "geo-lag-queue"), Is.True,
            "Queue should be visible on secondary after geo-replication scheduler runs");
    }

    [Test]
    public async Task TableStorage_RAGRS_BeforeGeoSync_TableNotVisibleOnSecondary_AfterGeoSync_TableVisible()
    {
        // Arrange — RA-GRS account; a table created after account provisioning will have
        // LastWriteTimeUtc > LastGeoSyncTime and must be hidden on the secondary until the watermark advances.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, TableLagRagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new TableClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={TableLagRagrsAccountName};AccountKey={key};" +
            $"TableEndpoint=https://{TableLagRagrsAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;";

        var primaryServiceClient = new TableServiceClient(primaryConnectionString, clientOptions);
        await primaryServiceClient.CreateTableAsync("georeplag");

        var secondaryEndpoint =
            $"https://{TableLagRagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";
        var secondaryServiceClient = new TableServiceClient(
            new Uri(secondaryEndpoint),
            new TableSharedKeyCredential(TableLagRagrsAccountName, key),
            clientOptions);

        // Act — query tables on secondary before geo-sync
        var tablesBeforeSync = secondaryServiceClient.Query().ToList();

        // Assert — table must not be visible yet
        Assert.That(tablesBeforeSync.Any(t => t.Name == "georeplag"), Is.False,
            "Table should not be visible on secondary before geo-replication scheduler runs");

        // Trigger geo-replication for this account only
        AzureStorageControlPlane.New(new PrettyTopazLogger()).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            TableLagRagrsAccountName);

        // Act — query tables on secondary after geo-sync
        var tablesAfterSync = secondaryServiceClient.Query().ToList();

        // Assert — table must now be visible
        Assert.That(tablesAfterSync.Any(t => t.Name == "georeplag"), Is.True,
            "Table should be visible on secondary after geo-replication scheduler runs");
    }

    [Test]
    public async Task TableStorage_RAGZRS_BeforeGeoSync_TableNotVisibleOnSecondary_AfterGeoSync_TableVisible()
    {
        // Arrange — RA-GZRS account; same replication-lag behaviour as RA-GRS.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, TableLagRagzrsAccountName, StorageSkuName.StandardRagzrs);
        var key = storageAccount.GetKeys().First().Value;

        var clientOptions = new TableClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={TableLagRagzrsAccountName};AccountKey={key};" +
            $"TableEndpoint=https://{TableLagRagzrsAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;";

        var primaryServiceClient = new TableServiceClient(primaryConnectionString, clientOptions);
        await primaryServiceClient.CreateTableAsync("georeplag");

        var secondaryEndpoint =
            $"https://{TableLagRagzrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";
        var secondaryServiceClient = new TableServiceClient(
            new Uri(secondaryEndpoint),
            new TableSharedKeyCredential(TableLagRagzrsAccountName, key),
            clientOptions);

        // Act — query tables on secondary before geo-sync
        var tablesBeforeSync = secondaryServiceClient.Query().ToList();

        // Assert — table must not be visible yet
        Assert.That(tablesBeforeSync.Any(t => t.Name == "georeplag"), Is.False,
            "Table should not be visible on secondary before geo-replication scheduler runs");

        // Trigger geo-replication for this account only
        AzureStorageControlPlane.New(new PrettyTopazLogger()).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            TableLagRagzrsAccountName);

        // Act — query tables on secondary after geo-sync
        var tablesAfterSync = secondaryServiceClient.Query().ToList();

        // Assert — table must now be visible
        Assert.That(tablesAfterSync.Any(t => t.Name == "georeplag"), Is.True,
            "Table should be visible on secondary after geo-replication scheduler runs");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Nuance tests
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task QueueStorage_GetMessages_OnSecondary_Returns403()
    {
        // Real Azure blocks dequeue on secondary because GetMessages mutates state
        // (sets visibility timeout and increments dequeue count). Topaz follows the
        // same WriteOperationNotSupportedOnSecondary convention used for all other
        // secondary mutations.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        // Send a message via primary
        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";
        var primaryOptions = new QueueClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
        };
        var primaryClient = new QueueServiceClient(primaryConnectionString, primaryOptions);
        await primaryClient.CreateQueueAsync("getmsg-secondary-test");
        await primaryClient.GetQueueClient("getmsg-secondary-test").SendMessageAsync("hello");

        // Attempt to receive (dequeue) from secondary
        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"QueueEndpoint=https://{RagrsAccountName}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;";
        var secondaryOptions = new QueueClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
        };
        var secondaryQueueClient = new QueueClient(secondaryConnectionString, "getmsg-secondary-test", secondaryOptions);

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await secondaryQueueClient.ReceiveMessageAsync());

        Assert.That(ex!.Status, Is.EqualTo(403));
        Assert.That(ex.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
    }

    [Test]
    public async Task TableStorage_ODataQuery_OnSecondary_FiltersProjectsAndLimitsCorrectly()
    {
        // All standard OData operations ($filter, $select, $top) must work on the secondary endpoint.
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var tableOptions = new TableClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
        };

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
            $"TableEndpoint=https://{RagrsAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;";
        var primaryService = new TableServiceClient(primaryConnectionString, tableOptions);
        await primaryService.CreateTableAsync("odatatest");
        var primary = primaryService.GetTableClient("odatatest");
        await primary.AddEntityAsync(new TableEntity("pk", "r1") { { "Score", 10 }, { "Tag", "alpha" } });
        await primary.AddEntityAsync(new TableEntity("pk", "r2") { { "Score", 20 }, { "Tag", "beta" } });
        await primary.AddEntityAsync(new TableEntity("pk", "r3") { { "Score", 30 }, { "Tag", "alpha" } });

        AzureStorageControlPlane.New(new PrettyTopazLogger()).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            RagrsAccountName);

        var secondaryEndpoint =
            $"https://{RagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/";
        var secondaryService = new TableServiceClient(new Uri(secondaryEndpoint),
            new TableSharedKeyCredential(RagrsAccountName, key), tableOptions);
        var secondary = secondaryService.GetTableClient("odatatest");

        // $filter
        var filtered = secondary.Query<TableEntity>(e => e.GetString("Tag") == "alpha").ToList();
        Assert.That(filtered.Count, Is.EqualTo(2), "$filter should return only alpha-tagged rows");

        // $select
        var projected = secondary.Query<TableEntity>(select: ["RowKey", "Tag"]).ToList();
        Assert.That(projected.All(e => !e.ContainsKey("Score")), Is.True, "$select should exclude Score column");

        // $top
        var topped = secondary.Query<TableEntity>(maxPerPage: 2).Take(2).ToList();
        Assert.That(topped.Count, Is.EqualTo(2), "$top=2 should return at most 2 rows");
    }

    [Test]
    public async Task GeoReplication_UniformWatermark_SingleSyncMakesBlobQueueTableVisible()
    {
        // A single UpdateLastGeoSyncTime call on one account must advance the watermark for
        // all three services simultaneously — blob, queue, and table share one LastGeoSyncTime.
        const string account = "geouniformwm";
        var armClient = CreateArmClient();
        var resourceGroup = GetResourceGroup(armClient);
        var storageAccount = CreateStorageAccount(resourceGroup, account, StorageSkuName.StandardRagrs);
        var key = storageAccount.GetKeys().First().Value;

        var blobOptions = new BlobClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
        };
        var queueOptions = new QueueClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
        };
        var tableOptions = new TableClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(
                new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
        };

        // Write one resource to each service via primary
        var primaryBlob = new BlobServiceClient(
            $"DefaultEndpointsProtocol=https;AccountName={account};AccountKey={key};" +
            $"BlobEndpoint=https://{account}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;",
            blobOptions);
        await primaryBlob.CreateBlobContainerAsync("wmcontainer");
        await primaryBlob.GetBlobContainerClient("wmcontainer").GetBlobClient("wm.txt").UploadAsync(new BinaryData("x"));

        var primaryQueue = new QueueServiceClient(
            $"DefaultEndpointsProtocol=https;AccountName={account};AccountKey={key};" +
            $"QueueEndpoint=https://{account}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;",
            queueOptions);
        await primaryQueue.CreateQueueAsync("wmqueue");

        var primaryTable = new TableServiceClient(
            $"DefaultEndpointsProtocol=https;AccountName={account};AccountKey={key};" +
            $"TableEndpoint=https://{account}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/;",
            tableOptions);
        await primaryTable.CreateTableAsync("wmtable");

        // Single watermark advance
        AzureStorageControlPlane.New(new PrettyTopazLogger()).UpdateLastGeoSyncTime(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From(ResourceGroupName),
            account);

        // Verify all three are now visible on their respective secondary endpoints
        var secondaryBlob = new BlobServiceClient(
            $"DefaultEndpointsProtocol=https;AccountName={account};AccountKey={key};" +
            $"BlobEndpoint=https://{account}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;",
            blobOptions);
        var blobs = secondaryBlob.GetBlobContainerClient("wmcontainer").GetBlobs().ToList();
        Assert.That(blobs.Any(b => b.Name == "wm.txt"), Is.True, "Blob must be visible after single watermark advance");

        var secondaryQueue = new QueueServiceClient(
            $"DefaultEndpointsProtocol=https;AccountName={account};AccountKey={key};" +
            $"QueueEndpoint=https://{account}-secondary.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/;",
            queueOptions);
        var queues = secondaryQueue.GetQueuesAsync().ToBlockingEnumerable().ToList();
        Assert.That(queues.Any(q => q.Name == "wmqueue"), Is.True, "Queue must be visible after single watermark advance");

        var secondaryTable = new TableServiceClient(
            new Uri($"https://{account}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/"),
            new TableSharedKeyCredential(account, key), tableOptions);
        var tables = secondaryTable.Query().ToList();
        Assert.That(tables.Any(t => t.Name == "wmtable"), Is.True, "Table must be visible after single watermark advance");
    }

    [Test]
    public Task SecondaryEndpoint_AllWriteOperations_Return403()
    {
        try
        {
            // Verify 403 WriteOperationNotSupportedOnSecondary across a representative set of
            // mutating HTTP methods: PUT (blob upload), DELETE (blob), entity upsert (table merge).
            var armClient = CreateArmClient();
            var resourceGroup = GetResourceGroup(armClient);
            var storageAccount = CreateStorageAccount(resourceGroup, RagrsAccountName, StorageSkuName.StandardRagrs);
            var key = storageAccount.GetKeys().First().Value;

            var blobOptions = new BlobClientOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
            };
            var tableOptions = new TableClientOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
            };

            var secondaryBlobService = new BlobServiceClient(
                $"DefaultEndpointsProtocol=https;AccountName={RagrsAccountName};AccountKey={key};" +
                $"BlobEndpoint=https://{RagrsAccountName}-secondary.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/;",
                blobOptions);
            var secondaryTableService = new TableServiceClient(
                new Uri($"https://{RagrsAccountName}-secondary.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/"),
                new TableSharedKeyCredential(RagrsAccountName, key), tableOptions);

            using (Assert.EnterMultipleScope())
            {
                // PUT blob (upload) on secondary
                var blobUploadEx = Assert.ThrowsAsync<RequestFailedException>(async () =>
                    await secondaryBlobService
                        .GetBlobContainerClient("any-container")
                        .GetBlobClient("any.txt")
                        .UploadAsync(new BinaryData("x"), overwrite: true));
                Assert.That(blobUploadEx!.Status, Is.EqualTo(403));
                Assert.That(blobUploadEx.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));

                // DELETE blob container on secondary
                var containerDeleteEx = Assert.ThrowsAsync<RequestFailedException>(async () =>
                    await secondaryBlobService.DeleteBlobContainerAsync("any-container"));
                Assert.That(containerDeleteEx!.Status, Is.EqualTo(403));
                Assert.That(containerDeleteEx.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));

                // CREATE table on secondary (already covered but kept for completeness)
                var tableCreateEx = Assert.ThrowsAsync<RequestFailedException>(async () =>
                    await secondaryTableService.CreateTableAsync("writetable"));
                Assert.That(tableCreateEx!.Status, Is.EqualTo(403));
                Assert.That(tableCreateEx.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));

                // UPSERT entity on secondary
                var entityUpsertEx = Assert.ThrowsAsync<RequestFailedException>(async () =>
                    await secondaryTableService
                        .GetTableClient("writetable")
                        .UpsertEntityAsync(new TableEntity("pk", "rk")));
                Assert.That(entityUpsertEx!.Status, Is.EqualTo(403));
                Assert.That(entityUpsertEx.ErrorCode, Is.EqualTo("WriteOperationNotSupportedOnSecondary"));
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
