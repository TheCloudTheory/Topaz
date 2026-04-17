using Topaz.CLI;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class BlobStorageTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("075A1FEB-A765-4170-899F-B9370412CC9D");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "blobstoragetests";

    private string _key = null!;
    
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

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "storage",
            "account",
            "delete",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "storage",
            "account",
            "create",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
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
    public void BlobStorageTests_WhenBlobPropertiesAreSet_TheyShouldBeRetrievable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("set-props-test");
        var containerClient = serviceClient.GetBlobContainerClient("set-props-test");
        var blobClient = containerClient.GetBlobClient("hello.txt");
        blobClient.Upload(new BinaryData("hello world"));

        // Act
        blobClient.SetHttpHeaders(new BlobHttpHeaders
        {
            ContentType = "text/plain",
            ContentEncoding = "utf-8",
            ContentLanguage = "en-US",
            CacheControl = "max-age=3600",
            ContentDisposition = "inline"
        });

        var properties = blobClient.GetProperties();

        // Assert
        Assert.That(properties.Value.ContentType, Is.EqualTo("text/plain"));
        Assert.That(properties.Value.ContentEncoding, Is.EqualTo("utf-8"));
        Assert.That(properties.Value.ContentLanguage, Is.EqualTo("en-US"));
        Assert.That(properties.Value.CacheControl, Is.EqualTo("max-age=3600"));
        Assert.That(properties.Value.ContentDisposition, Is.EqualTo("inline"));
    }

    [Test]
    public void BlobStorageTests_WhenBlobPropertiesAreRequested_ContentTypeAndLengthShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("props-test");
        var containerClient = serviceClient.GetBlobContainerClient("props-test");
        var blobClient = containerClient.GetBlobClient("hello.txt");
        var content = new BinaryData("hello world");
        blobClient.Upload(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } });

        // Act
        var properties = blobClient.GetProperties();

        // Assert
        Assert.That(properties.Value.ContentType, Is.EqualTo("text/plain"));
        Assert.That(properties.Value.ContentLength, Is.GreaterThan(0));
        Assert.That(properties.Value.BlobType, Is.EqualTo(BlobType.Block));
        Assert.That(properties.Value.LastModified, Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(properties.Value.ETag, Is.Not.EqualTo(default(ETag)));
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
    public void BlobStorageTests_WhenBlobPropertiesAreRequestedForNotExistentBlob_ProperErrorShouldBeThrownAndHandled()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("test");
        var containerClient = serviceClient.GetBlobContainerClient("test");
        var blobClient = containerClient.GetBlobClient("notexisting.txt");

        // Assert
        try
        {
            _ = blobClient.GetProperties();
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            Assert.Pass();
        }
        catch
        {
            Assert.Fail("Request failed with invalid error code.");
        }
    }

    [Test]
    public void BlobStorageTests_WhenBlobMetadataIsSet_ItShouldBeRetrievable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("metadata-retrieve-test");
        var containerClient = serviceClient.GetBlobContainerClient("metadata-retrieve-test");
        var blobClient = containerClient.GetBlobClient("test.txt");
        blobClient.Upload(new BinaryData("some content"));
        blobClient.SetMetadata(new Dictionary<string, string>
        {
            { "env", "prod" },
            { "version", "42" }
        });

        // Act
        var properties = blobClient.GetProperties();

        // Assert
        Assert.That(properties.Value.Metadata["env"], Is.EqualTo("prod"));
        Assert.That(properties.Value.Metadata["version"], Is.EqualTo("42"));
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

    [Test]
    public void BlobStorageTests_WhenContainerMetadataAreSet_TheyShouldBeAccepted()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("meta-test");
        var containerClient = serviceClient.GetBlobContainerClient("meta-test");

        // Act
        var info = containerClient.SetMetadata(new Dictionary<string, string>
        {
            { "env", "prod" },
            { "owner", "team-a" }
        });

        // Assert
        Assert.That(info, Is.Not.Null);
        Assert.That(info.GetRawResponse().Status, Is.EqualTo(200));
    }

    [Test]
    public void BlobStorageTests_WhenContainerMetadataAreSet_TheyShouldBeRetrievable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("meta-retrieve-test");
        var containerClient = serviceClient.GetBlobContainerClient("meta-retrieve-test");
        containerClient.SetMetadata(new Dictionary<string, string>
        {
            { "env", "prod" },
            { "owner", "team-a" }
        });

        // Act
        var props = containerClient.GetProperties();

        // Assert
        Assert.That(props.Value.Metadata["env"], Is.EqualTo("prod"));
        Assert.That(props.Value.Metadata["owner"], Is.EqualTo("team-a"));
    }

    [Test]
    public void BlobStorageTests_WhenContainerAclIsRequested_EmptySignedIdentifiersShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("acl-test");
        var containerClient = serviceClient.GetBlobContainerClient("acl-test");

        // Act
        var policy = containerClient.GetAccessPolicy();

        // Assert
        Assert.That(policy, Is.Not.Null);
        Assert.That(policy.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(policy.Value.SignedIdentifiers, Is.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenContainerAclIsSet_ItShouldBeRetrievable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("acl-set-test");
        var containerClient = serviceClient.GetBlobContainerClient("acl-set-test");

        var identifiers = new[]
        {
            new BlobSignedIdentifier
            {
                Id = "pol1",
                AccessPolicy = new BlobAccessPolicy
                {
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Permissions = "r"
                }
            }
        };

        // Act
        containerClient.SetAccessPolicy(PublicAccessType.None, identifiers);
        var policy = containerClient.GetAccessPolicy();

        // Assert
        var signedIdentifiers = policy.Value.SignedIdentifiers.ToArray();
        Assert.That(signedIdentifiers, Has.Length.EqualTo(1));
        Assert.That(signedIdentifiers[0].Id, Is.EqualTo("pol1"));
        Assert.That(signedIdentifiers[0].AccessPolicy.Permissions, Is.EqualTo("r"));
    }

    [Test]
    public void BlobStorageTests_WhenContainerLeaseIsAcquired_ItShouldSucceed()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("lease-acquire-test");
        var containerClient = serviceClient.GetBlobContainerClient("lease-acquire-test");

        // Act
        var leaseClient = containerClient.GetBlobLeaseClient();
        var leaseResponse = leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(leaseResponse, Is.Not.Null);
        Assert.That(leaseResponse.GetRawResponse().Status, Is.EqualTo(201));
        Assert.That(leaseResponse.Value.LeaseId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenContainerLeaseIsRenewed_ItShouldSucceed()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("lease-renew-test");
        var containerClient = serviceClient.GetBlobContainerClient("lease-renew-test");
        var leaseClient = containerClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act
        var renewResponse = leaseClient.Renew();

        // Assert
        Assert.That(renewResponse, Is.Not.Null);
        Assert.That(renewResponse.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(renewResponse.Value.LeaseId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenContainerLeaseIsChanged_NewLeaseIdShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("lease-change-test");
        var containerClient = serviceClient.GetBlobContainerClient("lease-change-test");
        var leaseClient = containerClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        var newLeaseId = Guid.NewGuid().ToString();

        // Act
        var changeResponse = leaseClient.Change(newLeaseId);

        // Assert
        Assert.That(changeResponse, Is.Not.Null);
        Assert.That(changeResponse.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(changeResponse.Value.LeaseId, Is.EqualTo(newLeaseId));
    }

    [Test]
    public void BlobStorageTests_WhenContainerLeaseIsReleased_ContainerShouldBeAvailable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("lease-release-test");
        var containerClient = serviceClient.GetBlobContainerClient("lease-release-test");
        var leaseClient = containerClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act
        var releaseResponse = leaseClient.Release();

        // Assert
        Assert.That(releaseResponse, Is.Not.Null);
        Assert.That(releaseResponse.GetRawResponse().Status, Is.EqualTo(200));
    }

    [Test]
    public void BlobStorageTests_WhenContainerLeaseIsBroken_ItShouldSucceed()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("lease-break-test");
        var containerClient = serviceClient.GetBlobContainerClient("lease-break-test");
        var leaseClient = containerClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act
        var breakResponse = leaseClient.Break(TimeSpan.Zero);

        // Assert
        Assert.That(breakResponse, Is.Not.Null);
        Assert.That(breakResponse.GetRawResponse().Status, Is.EqualTo(202));
    }

    [Test]
    public void BlobStorageTests_WhenAcquiringLeaseOnAlreadyLeasedContainer_ConflictShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("lease-conflict-test");
        var containerClient = serviceClient.GetBlobContainerClient("lease-conflict-test");
        var leaseClient = containerClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act & Assert
        var secondLeaseClient = containerClient.GetBlobLeaseClient();
        try
        {
            secondLeaseClient.Acquire(TimeSpan.FromSeconds(30));
            Assert.Fail("Expected RequestFailedException was not thrown.");
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void BlobStorageTests_WhenBlobIsCopied_DestinationShouldHaveSameContent()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("copy-source");
        serviceClient.CreateBlobContainer("copy-dest");
        var sourceBlob = serviceClient.GetBlobContainerClient("copy-source").GetBlobClient("original.txt");
        sourceBlob.Upload(new BinaryData("hello copy world"));

        // Act
        var destBlob = serviceClient.GetBlobContainerClient("copy-dest").GetBlobClient("copied.txt");
        destBlob.StartCopyFromUri(sourceBlob.Uri).WaitForCompletion();

        // Assert
        var download = destBlob.DownloadContent();
        Assert.That(download.Value.Content.ToString(), Is.EqualTo("hello copy world"));
    }

    [Test]
    public void BlobStorageTests_WhenBlobIsCopied_DestinationPropertiesShouldMatch()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("copy-props-src");
        serviceClient.CreateBlobContainer("copy-props-dst");
        var sourceBlob = serviceClient.GetBlobContainerClient("copy-props-src").GetBlobClient("source.txt");
        sourceBlob.Upload(new BinaryData("properties test"),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } });

        // Act
        var destBlob = serviceClient.GetBlobContainerClient("copy-props-dst").GetBlobClient("dest.txt");
        destBlob.StartCopyFromUri(sourceBlob.Uri).WaitForCompletion();
        var properties = destBlob.GetProperties();

        // Assert
        Assert.That(properties.Value.ContentType, Is.EqualTo("text/plain"));
        Assert.That(properties.Value.ContentLength, Is.GreaterThan(0));
    }

    [Test]
    public void BlobStorageTests_WhenCopyingNonExistentBlob_NotFoundShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("copy-nf-src");
        serviceClient.CreateBlobContainer("copy-nf-dst");
        var sourceBlob = serviceClient.GetBlobContainerClient("copy-nf-src").GetBlobClient("does-not-exist.txt");
        var destBlob = serviceClient.GetBlobContainerClient("copy-nf-dst").GetBlobClient("dest.txt");

        // Assert
        Assert.Throws<RequestFailedException>(() => destBlob.StartCopyFromUri(sourceBlob.Uri));
    }

    [Test]
    public void BlobStorageTests_WhenBlockIsStaged_ItShouldReturn201()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("block-test");
        var blockBlobClient = serviceClient.GetBlobContainerClient("block-test")
            .GetBlockBlobClient("staged.txt");

        var blockId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("block-001"));
        var blockContent = BinaryData.FromString("hello block world");

        // Act
        var response = blockBlobClient.StageBlock(blockId, blockContent.ToStream());

        // Assert
        Assert.That(response.GetRawResponse().Status, Is.EqualTo(201));
    }

    [Test]
    public void BlobStorageTests_WhenMultipleBlocksAreStaged_AllShouldReturn201()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("multiblock-test");
        var blockBlobClient = serviceClient.GetBlobContainerClient("multiblock-test")
            .GetBlockBlobClient("multi.txt");

        // Act & Assert
        for (var i = 0; i < 3; i++)
        {
            var blockId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"block-{i:D3}"));
            var response = blockBlobClient.StageBlock(blockId, BinaryData.FromString($"chunk {i}").ToStream());
            Assert.That(response.GetRawResponse().Status, Is.EqualTo(201));
        }
    }

    [Test]
    public void BlobStorageTests_WhenBlockListIsCommitted_BlobShouldBeDownloadable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blocklist-test");
        var blockBlobClient = serviceClient.GetBlobContainerClient("blocklist-test")
            .GetBlockBlobClient("assembled.txt");

        var blocks = new[] { "Hello, ", "block ", "world!" };
        var blockIds = blocks
            .Select((_, i) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"blk-{i:D3}")))
            .ToList();

        for (var i = 0; i < blocks.Length; i++)
            blockBlobClient.StageBlock(blockIds[i], BinaryData.FromString(blocks[i]).ToStream());

        // Act
        var commitResponse = blockBlobClient.CommitBlockList(blockIds);

        // Assert
        Assert.That(commitResponse.GetRawResponse().Status, Is.EqualTo(201));

        var download = blockBlobClient.DownloadContent();
        Assert.That(download.Value.Content.ToString(), Is.EqualTo("Hello, block world!"));
    }

    [Test]
    public void BlobStorageTests_WhenBlockListIsCommitted_PropertiesShouldReflectAssembledBlob()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blocklist-props-test");
        var blockBlobClient = serviceClient.GetBlobContainerClient("blocklist-props-test")
            .GetBlockBlobClient("props.txt");

        var blockId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("only-block"));
        blockBlobClient.StageBlock(blockId, BinaryData.FromString("content data").ToStream());

        // Act
        blockBlobClient.CommitBlockList([blockId]);
        var properties = blockBlobClient.GetProperties();

        // Assert
        Assert.That(properties.Value.ContentLength, Is.GreaterThan(0));
        Assert.That(properties.Value.ETag, Is.Not.EqualTo(default(ETag)));
        Assert.That(properties.Value.BlobType, Is.EqualTo(BlobType.Block));
    }

    [Test]
    public void BlobStorageTests_WhenBlocksAreStaged_GetBlockListShouldReturnUncommittedBlocks()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("get-blocklist-uncommitted");
        var blockBlobClient = serviceClient.GetBlobContainerClient("get-blocklist-uncommitted")
            .GetBlockBlobClient("staged-only.txt");

        var blockId1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("block-a"));
        var blockId2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("block-b"));
        blockBlobClient.StageBlock(blockId1, BinaryData.FromString("hello ").ToStream());
        blockBlobClient.StageBlock(blockId2, BinaryData.FromString("world").ToStream());

        // Act
        var blockList = blockBlobClient.GetBlockList(BlockListTypes.Uncommitted);

        // Assert
        Assert.That(blockList.Value.UncommittedBlocks.Count, Is.EqualTo(2));
        Assert.That(blockList.Value.UncommittedBlocks.Select(b => b.Name),
            Is.EquivalentTo(new[] { blockId1, blockId2 }));
        Assert.That(blockList.Value.UncommittedBlocks.All(b => b.SizeLong > 0), Is.True);
        Assert.That(blockList.Value.CommittedBlocks, Is.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenBlockListIsCommitted_GetBlockListShouldReturnCommittedBlocks()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("get-blocklist-committed");
        var blockBlobClient = serviceClient.GetBlobContainerClient("get-blocklist-committed")
            .GetBlockBlobClient("committed.txt");

        var blocks = new[] { "Hello, ", "block ", "world!" };
        var blockIds = blocks
            .Select((_, i) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"blk-{i:D3}")))
            .ToArray();

        for (var i = 0; i < blocks.Length; i++)
            blockBlobClient.StageBlock(blockIds[i], BinaryData.FromString(blocks[i]).ToStream());

        blockBlobClient.CommitBlockList(blockIds);

        // Act
        var blockList = blockBlobClient.GetBlockList(BlockListTypes.Committed);

        // Assert
        Assert.That(blockList.Value.CommittedBlocks.Count, Is.EqualTo(3));
        Assert.That(blockList.Value.CommittedBlocks.Select(b => b.Name),
            Is.EqualTo(blockIds));
        Assert.That(blockList.Value.CommittedBlocks.All(b => b.SizeLong > 0), Is.True);
        Assert.That(blockList.Value.UncommittedBlocks, Is.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenBlockListTypeIsAll_GetBlockListShouldReturnBothCommittedAndUncommitted()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("get-blocklist-all");
        var blockBlobClient = serviceClient.GetBlobContainerClient("get-blocklist-all")
            .GetBlockBlobClient("mixed.txt");

        var committedId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("committed-blk"));
        var uncommittedId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("uncommitted-blk"));

        blockBlobClient.StageBlock(committedId, BinaryData.FromString("committed content").ToStream());
        blockBlobClient.CommitBlockList([committedId]);

        blockBlobClient.StageBlock(uncommittedId, BinaryData.FromString("pending content").ToStream());

        // Act
        var blockList = blockBlobClient.GetBlockList(BlockListTypes.All);

        // Assert
        Assert.That(blockList.Value.CommittedBlocks.Count, Is.EqualTo(1));
        Assert.That(blockList.Value.CommittedBlocks.First().Name, Is.EqualTo(committedId));
        Assert.That(blockList.Value.UncommittedBlocks.Count, Is.EqualTo(1));
        Assert.That(blockList.Value.UncommittedBlocks.First().Name, Is.EqualTo(uncommittedId));
    }

    [Test]
    public void BlobStorageTests_WhenPageBlobIsCreated_ItShouldHaveCorrectProperties()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-blob-create");
        var pageBlob = serviceClient.GetBlobContainerClient("page-blob-create")
            .GetPageBlobClient("test-page.bin");

        // Act
        pageBlob.Create(512);
        var properties = pageBlob.GetProperties();

        // Assert
        Assert.That(properties.Value.BlobType, Is.EqualTo(BlobType.Page));
        Assert.That(properties.Value.ContentLength, Is.EqualTo(512));
    }

    [Test]
    public void BlobStorageTests_WhenPagesAreUploaded_DownloadedContentShouldMatch()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-blob-upload");
        var pageBlob = serviceClient.GetBlobContainerClient("page-blob-upload")
            .GetPageBlobClient("uploaded-page.bin");

        pageBlob.Create(512);

        var pageContent = new byte[512];
        for (var i = 0; i < pageContent.Length; i++)
            pageContent[i] = (byte)(i % 256);

        // Act
        pageBlob.UploadPages(new BinaryData(pageContent).ToStream(), 0);
        var download = pageBlob.DownloadContent();

        // Assert
        Assert.That(download.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(download.Value.Content.ToArray(), Is.EqualTo(pageContent));
    }

    [Test]
    public void BlobStorageTests_WhenPagesAreUploadedAtOffset_ContentShouldMatchAtThatOffset()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-blob-offset");
        var pageBlob = serviceClient.GetBlobContainerClient("page-blob-offset")
            .GetPageBlobClient("offset-page.bin");

        pageBlob.Create(1024);

        var pageContent = new byte[512];
        for (var i = 0; i < pageContent.Length; i++)
            pageContent[i] = 0xAB;

        // Act: write to the second page (offset 512)
        pageBlob.UploadPages(new BinaryData(pageContent).ToStream(), 512);
        var download = pageBlob.DownloadContent();
        var downloaded = download.Value.Content.ToArray();

        // Assert: first page should be zeros, second page should match our content
        Assert.That(downloaded.Length, Is.EqualTo(1024));
        Assert.That(downloaded[..512], Is.All.EqualTo(0));
        Assert.That(downloaded[512..], Is.EqualTo(pageContent));
    }

    [Test]
    public void BlobStorageTests_WhenPageBlobPropertiesAreRequested_BlobTypeShouldBePageBlob()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-blob-props");
        var pageBlob = serviceClient.GetBlobContainerClient("page-blob-props")
            .GetPageBlobClient("props-page.bin");

        pageBlob.Create(512);
        var pageContent = new byte[512];
        pageBlob.UploadPages(new BinaryData(pageContent).ToStream(), 0);

        // Act
        var properties = pageBlob.GetProperties();

        // Assert
        Assert.That(properties.Value.BlobType, Is.EqualTo(BlobType.Page));
        Assert.That(properties.Value.ContentLength, Is.EqualTo(512));
        Assert.That(properties.Value.ETag, Is.Not.EqualTo(default(ETag)));
        Assert.That(properties.Value.LastModified, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public void BlobStorageTests_WhenPageRangesAreRequested_ValidRangesShouldBeReturned()
    {
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-ranges");
        var pageBlob = serviceClient.GetBlobContainerClient("page-ranges")
            .GetPageBlobClient("ranges-page.bin");

        pageBlob.Create(1024);
        pageBlob.UploadPages(new BinaryData(new byte[512]).ToStream(), 0);
        pageBlob.UploadPages(new BinaryData(new byte[512]).ToStream(), 512);

        var response = pageBlob.GetPageRanges();
        var ranges = response.Value.PageRanges.ToArray();

        Assert.That(response.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(ranges, Has.Length.EqualTo(1));
        Assert.That(ranges[0].Offset, Is.EqualTo(0));
        Assert.That(ranges[0].Length, Is.EqualTo(1024));
    }

    [Test]
    public void BlobStorageTests_WhenPageRangesAreRequestedForSubset_OnlyIntersectingRangesShouldBeReturned()
    {
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-ranges-subset");
        var pageBlob = serviceClient.GetBlobContainerClient("page-ranges-subset")
            .GetPageBlobClient("subset-page.bin");

        pageBlob.Create(1024);
        pageBlob.UploadPages(new BinaryData(new byte[512]).ToStream(), 0);
        pageBlob.UploadPages(new BinaryData(new byte[512]).ToStream(), 512);

        var response = pageBlob.GetPageRanges(new HttpRange(512, 512));
        var ranges = response.Value.PageRanges.ToArray();

        Assert.That(ranges, Has.Length.EqualTo(1));
        Assert.That(ranges[0].Offset, Is.EqualTo(512));
        Assert.That(ranges[0].Length, Is.EqualTo(512));
    }

    [Test]
    public void BlobStorageTests_WhenPagesAreCleared_TheClearedRangeShouldNotBeReturned()
    {
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("page-ranges-clear");
        var pageBlob = serviceClient.GetBlobContainerClient("page-ranges-clear")
            .GetPageBlobClient("clear-page.bin");

        pageBlob.Create(1024);
        pageBlob.UploadPages(new BinaryData(new byte[512]).ToStream(), 0);
        pageBlob.UploadPages(new BinaryData(new byte[512]).ToStream(), 512);
        pageBlob.ClearPages(new HttpRange(0, 512));

        var response = pageBlob.GetPageRanges();
        var ranges = response.Value.PageRanges.ToArray();

        Assert.That(ranges, Has.Length.EqualTo(1));
        Assert.That(ranges[0].Offset, Is.EqualTo(512));
        Assert.That(ranges[0].Length, Is.EqualTo(512));
    }

    [Test]
    public void BlobStorageTests_WhenBlobLeaseIsAcquired_ItShouldSucceed()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blob-lease-acquire");
        var blobClient = serviceClient.GetBlobContainerClient("blob-lease-acquire").GetBlobClient("lease-test.txt");
        blobClient.Upload(BinaryData.FromString("hello").ToStream());

        // Act
        var leaseClient = blobClient.GetBlobLeaseClient();
        var leaseResponse = leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(leaseResponse, Is.Not.Null);
        Assert.That(leaseResponse.GetRawResponse().Status, Is.EqualTo(201));
        Assert.That(leaseResponse.Value.LeaseId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenBlobLeaseIsRenewed_ItShouldSucceed()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blob-lease-renew");
        var blobClient = serviceClient.GetBlobContainerClient("blob-lease-renew").GetBlobClient("lease-renew.txt");
        blobClient.Upload(BinaryData.FromString("hello").ToStream());
        var leaseClient = blobClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act
        var renewResponse = leaseClient.Renew();

        // Assert
        Assert.That(renewResponse, Is.Not.Null);
        Assert.That(renewResponse.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(renewResponse.Value.LeaseId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void BlobStorageTests_WhenBlobLeaseIsChanged_NewLeaseIdShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blob-lease-change");
        var blobClient = serviceClient.GetBlobContainerClient("blob-lease-change").GetBlobClient("lease-change.txt");
        blobClient.Upload(BinaryData.FromString("hello").ToStream());
        var leaseClient = blobClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        var newLeaseId = Guid.NewGuid().ToString();

        // Act
        var changeResponse = leaseClient.Change(newLeaseId);

        // Assert
        Assert.That(changeResponse, Is.Not.Null);
        Assert.That(changeResponse.GetRawResponse().Status, Is.EqualTo(200));
        Assert.That(changeResponse.Value.LeaseId, Is.EqualTo(newLeaseId));
    }

    [Test]
    public void BlobStorageTests_WhenBlobLeaseIsReleased_BlobShouldBeAvailable()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blob-lease-release");
        var blobClient = serviceClient.GetBlobContainerClient("blob-lease-release").GetBlobClient("lease-release.txt");
        blobClient.Upload(BinaryData.FromString("hello").ToStream());
        var leaseClient = blobClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act
        var releaseResponse = leaseClient.Release();

        // Assert
        Assert.That(releaseResponse, Is.Not.Null);
        Assert.That(releaseResponse.GetRawResponse().Status, Is.EqualTo(200));
    }

    [Test]
    public void BlobStorageTests_WhenBlobLeaseIsBroken_ItShouldSucceed()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blob-lease-break");
        var blobClient = serviceClient.GetBlobContainerClient("blob-lease-break").GetBlobClient("lease-break.txt");
        blobClient.Upload(BinaryData.FromString("hello").ToStream());
        var leaseClient = blobClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act
        var breakResponse = leaseClient.Break(TimeSpan.Zero);

        // Assert
        Assert.That(breakResponse, Is.Not.Null);
        Assert.That(breakResponse.GetRawResponse().Status, Is.EqualTo(202));
    }

    [Test]
    public void BlobStorageTests_WhenAcquiringLeaseOnAlreadyLeasedBlob_ConflictShouldBeReturned()
    {
        // Arrange
        var serviceClient = new BlobServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        serviceClient.CreateBlobContainer("blob-lease-conflict");
        var blobClient = serviceClient.GetBlobContainerClient("blob-lease-conflict").GetBlobClient("lease-conflict.txt");
        blobClient.Upload(BinaryData.FromString("hello").ToStream());
        var leaseClient = blobClient.GetBlobLeaseClient();
        leaseClient.Acquire(TimeSpan.FromSeconds(30));

        // Act & Assert
        var secondLeaseClient = blobClient.GetBlobLeaseClient();
        try
        {
            secondLeaseClient.Acquire(TimeSpan.FromSeconds(30));
            Assert.Fail("Expected RequestFailedException was not thrown.");
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            Assert.Pass();
        }
    }
}
