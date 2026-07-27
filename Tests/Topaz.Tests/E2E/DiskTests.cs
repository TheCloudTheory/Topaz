using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class DiskTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234560099");

    private static readonly HttpClient HttpClient = new();

    private const string SubscriptionName = "sub-test-disk";
    private const string ResourceGroupName = "rg-test-disk";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group", "delete",
            "--name", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }
    
    [OneTimeTearDown]
    public static void TearDown() => HttpClient.Dispose();

    private static ManagedDiskData MinimalDiskData() =>
        new(AzureLocation.WestEurope)
        {
            Sku = new DiskSku { Name = DiskStorageAccountType.PremiumLrs },
            DiskSizeGB = 32,
            CreationData = new DiskCreationData(DiskCreateOption.Empty)
        };

    [Test]
    public async Task DiskTests_WhenDiskIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-create";

        // Act
        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        var disk = createResult.Value;

        // Assert
        Assert.That(disk, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(disk.Data.Name, Is.EqualTo(diskName));
            Assert.That(disk.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Compute/disks")));
            Assert.That(disk.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(disk.Data.ProvisioningState, Is.EqualTo("Succeeded"));
        }
    }

    [Test]
    public async Task DiskTests_WhenDiskIsRetrievedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-get";

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        // Act
        var disk = await resourceGroup.Value.GetManagedDiskAsync(diskName);

        // Assert
        Assert.That(disk.Value.Data.Name, Is.EqualTo(diskName));
    }

    [Test]
    public async Task DiskTests_WhenDiskIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-delete";

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        // Act
        var disk = resourceGroup.Value.GetManagedDisk(diskName);
        await disk.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetManagedDiskAsync(diskName));
    }

    [Test]
    public async Task DiskTests_WhenDiskTagsAreUpdatedUsingSDK_TagsShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-update";

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());

        // Act
        var patch = new ManagedDiskPatch();
        patch.Tags.Add("env", "test");
        patch.Tags.Add("team", "platform");

        var disk = resourceGroup.Value.GetManagedDisk(diskName);
        var updateResult = await disk.Value.UpdateAsync(WaitUntil.Completed, patch);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(updateResult.Value.Data.Tags.ContainsKey("env"), Is.True);
            Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("test"));
            Assert.That(updateResult.Value.Data.Tags["team"], Is.EqualTo("platform"));
        }
    }

    [Test]
    public async Task DiskTests_WhenDisksAreListedByResourceGroupUsingSDK_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-disk-list-a", MinimalDiskData());
        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-disk-list-b", MinimalDiskData());

        // Act
        var disks = resourceGroup.Value.GetManagedDisks().GetAll().ToList();

        // Assert
        var names = disks.Select(d => d.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-disk-list-a"));
            Assert.That(names, Does.Contain("test-disk-list-b"));
        });
    }

    [Test]
    public async Task DiskTests_WhenDisksAreListedBySubscriptionUsingSDK_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-disk-sub-a", MinimalDiskData());

        // Act
        var disks = subscription.GetManagedDisks().ToList();

        // Assert
        var names = disks.Select(d => d.Data.Name).ToList();
        Assert.That(names, Does.Contain("test-disk-sub-a"));
    }

    [Test]
    public async Task DiskTests_WhenAccessIsGranted_ItShouldReturnAccessSasUri()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-grant-access";

        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());
        var disk = createResult.Value;

        // Act
        var grantResult = await disk.GrantAccessAsync(
            WaitUntil.Completed,
            new GrantAccessData(AccessLevel.Read, 3600));

        // Assert
        Assert.That(grantResult.Value.AccessSas, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task DiskTests_WhenAccessIsRevoked_ItShouldSucceed()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-revoke-access";

        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());
        var disk = createResult.Value;

        await disk.GrantAccessAsync(WaitUntil.Completed, new GrantAccessData(AccessLevel.Read, 3600));

        // Act + Assert (no exception = success)
        await disk.RevokeAccessAsync(WaitUntil.Completed);
    }

    [Test]
    public async Task DiskTests_WhenAccessIsGrantedOnActiveSasDisk_ItShouldReturn409()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-double-grant";

        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());
        var disk = createResult.Value;

        await disk.GrantAccessAsync(WaitUntil.Completed, new GrantAccessData(AccessLevel.Read, 3600));

        // Act + Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await disk.GrantAccessAsync(WaitUntil.Completed, new GrantAccessData(AccessLevel.Read, 3600)));
    }

    [Test]
    public async Task DiskTests_WhenAccessIsGranted_HeadShouldReturnCorrectContentLength()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-head";
        const long diskSizeGb = 1;

        var diskData = new ManagedDiskData(AzureLocation.WestEurope)
        {
            Sku = new DiskSku { Name = DiskStorageAccountType.PremiumLrs },
            DiskSizeGB = (int?)diskSizeGb,
            CreationData = new DiskCreationData(DiskCreateOption.Empty)
        };

        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, diskData);
        var disk = createResult.Value;

        var grantResult = await disk.GrantAccessAsync(
            WaitUntil.Completed, new GrantAccessData(AccessLevel.Read, 3600));
        var sasUri = new Uri(grantResult.Value.AccessSas!);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Head, sasUri);
        var headResponse = await HttpClient.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(headResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(headResponse.Content.Headers.ContentLength,
                Is.EqualTo(diskSizeGb * 1024L * 1024L * 1024L));
        }

        await disk.RevokeAccessAsync(WaitUntil.Completed);
    }

    [Test]
    public async Task DiskTests_WhenDataIsUploaded_ItCanBeDownloadedByRange()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-upload-download";

        var diskData = new ManagedDiskData(AzureLocation.WestEurope)
        {
            Sku = new DiskSku { Name = DiskStorageAccountType.PremiumLrs },
            DiskSizeGB = 1,
            CreationData = new DiskCreationData(DiskCreateOption.Empty)
        };

        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, diskData);
        var disk = createResult.Value;

        var grantResult = await disk.GrantAccessAsync(
            WaitUntil.Completed, new GrantAccessData(AccessLevel.Read, 3600));
        var sasUri = new Uri(grantResult.Value.AccessSas!);

        // Upload 512 bytes at offset 0
        var uploadData = new byte[512];
        for (var i = 0; i < uploadData.Length; i++)
            uploadData[i] = (byte)(i % 256);

        var putContent = new ByteArrayContent(uploadData);
        putContent.Headers.Add("Content-Range", "bytes 0-511/1073741824");
        var putRequest = new HttpRequestMessage(HttpMethod.Put, sasUri)
        {
            Content = putContent
        };
        var putResponse = await HttpClient.SendAsync(putRequest);
        Assert.That(putResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        // Download the same range
        var getRequest = new HttpRequestMessage(HttpMethod.Get, sasUri);
        getRequest.Headers.Add("Range", "bytes=0-511");
        var getResponse = await HttpClient.SendAsync(getRequest);
        Assert.That(getResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.PartialContent));

        var downloaded = await getResponse.Content.ReadAsByteArrayAsync();
        Assert.That(downloaded, Is.EqualTo(uploadData));

        await disk.RevokeAccessAsync(WaitUntil.Completed);
    }

    [Test]
    public async Task DiskTests_AfterRevokeAccess_SasEndpointReturns404()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string diskName = "test-disk-revoke-404";

        var createResult = await resourceGroup.Value.GetManagedDisks()
            .CreateOrUpdateAsync(WaitUntil.Completed, diskName, MinimalDiskData());
        var disk = createResult.Value;

        var grantResult = await disk.GrantAccessAsync(
            WaitUntil.Completed, new GrantAccessData(AccessLevel.Read, 3600));
        var sasUri = new Uri(grantResult.Value.AccessSas!);

        await disk.RevokeAccessAsync(WaitUntil.Completed);

        // Act
        var getResponse = await HttpClient.GetAsync(sasUri);

        // Assert
        Assert.That(getResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }
}
