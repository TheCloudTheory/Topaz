using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ApiManagementBackendTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003600");
    private const string SubscriptionName = "sub-test-apim-backend";
    private const string ResourceGroupName = "rg-test-apim-backend";
    private const string ServiceName = "apim-backend-tests";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static ApiManagementServiceData MinimalServiceData() =>
        new(AzureLocation.WestEurope,
            new ApiManagementServiceSkuProperties(ApiManagementServiceSkuType.Developer, 1),
            "admin@example.com",
            "Test Publisher");

    private static ApiManagementBackendData MinimalBackendData(string url = "https://backend.example.com") =>
        new() { Uri = new Uri(url), Protocol = BackendProtocol.Http };

    private async Task<ApiManagementServiceResource> CreateServiceAsync(ArmClient armClient)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;
        var result = await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, ServiceName, MinimalServiceData());
        return result.Value;
    }

    [Test]
    public async Task Backend_WhenCreated_HasCorrectProperties()
    {
        const string backendId = "backend-create-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var result = await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData("https://create.example.com"));

        Assert.That(result.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(backendId));
            Assert.That(result.Value.Data.Uri, Is.EqualTo(new Uri("https://create.example.com")));
            Assert.That(result.Value.Data.Protocol, Is.EqualTo(BackendProtocol.Http));
        }
    }

    [Test]
    public async Task Backend_WhenCreatedTwice_SecondCallRequiresIfMatch()
    {
        const string backendId = "backend-upsert-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData());

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementBackends()
                .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData()));

        Assert.That(ex!.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Backend_WhenRetrieved_ReturnsCorrectBackend()
    {
        const string backendId = "backend-get-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData("https://get.example.com"));

        var result = await service.GetApiManagementBackendAsync(backendId);

        Assert.That(result.Value.Data.Name, Is.EqualTo(backendId));
        Assert.That(result.Value.Data.Uri, Is.EqualTo(new Uri("https://get.example.com")));
    }

    [Test]
    public async Task Backend_WhenNotFound_GetThrows404()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementBackendAsync("nonexistent-backend"));
    }

    [Test]
    public async Task Backend_WhenUpdated_ReflectsNewUri()
    {
        const string backendId = "backend-update-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData("https://original.example.com"))).Value;

        var updated = await created.UpdateAsync(ETag.All, new ApiManagementBackendPatch
        {
            Uri = new Uri("https://updated.example.com")
        });

        Assert.That(updated.Value.Data.Uri, Is.EqualTo(new Uri("https://updated.example.com")));
    }

    [Test]
    public async Task Backend_WhenUpdatedWithMatchingETag_Succeeds()
    {
        const string backendId = "backend-update-etag-match";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData())).Value;

        var etagResponse = await created.GetEntityTagAsync();
        var eTag = etagResponse.GetRawResponse().Headers.ETag.GetValueOrDefault();
        var updated = await created.UpdateAsync(eTag, new ApiManagementBackendPatch
        {
            Uri = new Uri("https://etag-updated.example.com")
        });

        Assert.That(updated.Value.Data.Uri, Is.EqualTo(new Uri("https://etag-updated.example.com")));
    }

    [Test]
    public async Task Backend_WhenUpdatedWithWrongETag_Throws409()
    {
        const string backendId = "backend-update-wrong-etag";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData())).Value;

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await created.UpdateAsync(new ETag("\"wrong-etag-value\""), new ApiManagementBackendPatch
            {
                Uri = new Uri("https://should-fail.example.com")
            }));

        Assert.That(ex!.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Backend_WhenUpdatedAndNotFound_Throws404()
    {
        var armClient = CreateArmClient();
        _ = await CreateServiceAsync(armClient);

        var fakeBackend = armClient.GetApiManagementBackendResource(
            ApiManagementBackendResource.CreateResourceIdentifier(
                SubscriptionId.ToString(), ResourceGroupName, ServiceName, "nonexistent-backend"));

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await fakeBackend.UpdateAsync(ETag.All, new ApiManagementBackendPatch
            {
                Uri = new Uri("https://ghost.example.com")
            }));
    }

    [Test]
    public async Task Backend_WhenDeleted_IsNoLongerRetrievable()
    {
        const string backendId = "backend-delete-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var backend = (await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData())).Value;

        await backend.DeleteAsync(WaitUntil.Completed, ETag.All);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementBackendAsync(backendId));
    }

    [Test]
    public async Task Backend_List_ContainsCreatedBackends()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementBackends().CreateOrUpdateAsync(WaitUntil.Completed, "backend-list-1", MinimalBackendData("https://list1.example.com"));
        await service.GetApiManagementBackends().CreateOrUpdateAsync(WaitUntil.Completed, "backend-list-2", MinimalBackendData("https://list2.example.com"));

        var backends = service.GetApiManagementBackends().GetAll().Select(b => b.Data.Name).ToList();

        Assert.That(backends, Does.Contain("backend-list-1"));
        Assert.That(backends, Does.Contain("backend-list-2"));
    }

    [Test]
    public async Task Backend_WhenParentServiceNotFound_CreateThrows404()
    {
        var armClient = CreateArmClient();

        var fakeService = armClient.GetApiManagementServiceResource(
            ApiManagementServiceResource.CreateResourceIdentifier(
                SubscriptionId.ToString(), ResourceGroupName, "nonexistent-service"));

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await fakeService.GetApiManagementBackends()
                .CreateOrUpdateAsync(WaitUntil.Completed, "any-backend", MinimalBackendData()));
    }

    [Test]
    public async Task Backend_Reconnect_ReturnsAccepted()
    {
        const string backendId = "backend-reconnect-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var backend = (await service.GetApiManagementBackends()
            .CreateOrUpdateAsync(WaitUntil.Completed, backendId, MinimalBackendData())).Value;

        // Reconnect is a fire-and-forget POST that returns 202 Accepted
        Assert.DoesNotThrowAsync(async () => await backend.ReconnectAsync());
    }
}
