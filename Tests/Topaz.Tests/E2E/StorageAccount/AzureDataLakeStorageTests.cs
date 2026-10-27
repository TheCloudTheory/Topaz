using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E.StorageAccount;

internal sealed class AzureDataLakeStorageTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("075A1FEB-A765-4170-899F-B9370412CC9D");
    
    private const string SubscriptionName = "sub-adls-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "adlsstoragetests";

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
            SubscriptionId.ToString(),
            "--enable-hierarchical-namespace"
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
    public async Task AzureDataLakeStorage_CreateFileSystem_ReturnsCreated()
    {
        var serviceClient = CreateDataLakeServiceClient();

        var result = await serviceClient.CreateFileSystemAsync("test-filesystem");

        Assert.That(result.GetRawResponse().Status, Is.EqualTo(201));
    }

    [Test]
    public async Task AzureDataLakeStorage_CreateExistingFileSystem_ReturnsConflict()
    {
        var serviceClient = CreateDataLakeServiceClient();
        await serviceClient.CreateFileSystemAsync("duplicate-filesystem");

        var exception = Assert.ThrowsAsync<RequestFailedException>(
            async () => await serviceClient.CreateFileSystemAsync("duplicate-filesystem"));

        Assert.That(exception!.Status, Is.EqualTo(409));
    }

    [TestCase("ab")]
    [TestCase("invalid-")]
    [TestCase("invalid--name")]
    [TestCase("-invalid")]
    [TestCase("InvalidName")]
    public void AzureDataLakeStorage_CreateFileSystemWithInvalidName_ReturnsBadRequest(string filesystem)
    {
        var serviceClient = CreateDataLakeServiceClient();

        var exception = Assert.ThrowsAsync<RequestFailedException>(
            async () => await serviceClient.CreateFileSystemAsync(filesystem));

        Assert.That(exception!.Status, Is.EqualTo(400));
    }

    private DataLakeServiceClient CreateDataLakeServiceClient()
    {
        var serviceUri = new Uri(GlobalSettings.GetAdlsStorageEndpoint(StorageAccountName));

        return new DataLakeServiceClient(
            serviceUri,
            new StorageSharedKeyCredential(StorageAccountName, _key));
    }
}