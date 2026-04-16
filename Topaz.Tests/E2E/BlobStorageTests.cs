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
}