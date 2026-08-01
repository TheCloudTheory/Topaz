using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ApiManagementApiTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003400");
    private const string SubscriptionName = "sub-test-apim-api";
    private const string ResourceGroupName = "rg-test-apim-api";
    private const string ServiceName = "apim-api-tests";

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

    private static ApiCreateOrUpdateContent MinimalApiContent(string path = "test-api") =>
        new() { Path = path, DisplayName = "Test API" };

    private async Task<ApiManagementServiceResource> CreateServiceAsync(ArmClient armClient)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;
        var result = await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, ServiceName, MinimalServiceData());
        return result.Value;
    }

    [Test]
    public async Task Api_WhenCreated_HasCorrectProperties()
    {
        const string apiId = "api-create-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var result = await service.GetApis()
            .CreateOrUpdateAsync(WaitUntil.Completed, apiId, MinimalApiContent("create-path"));

        Assert.That(result.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(apiId));
            Assert.That(result.Value.Data.Path, Is.EqualTo("create-path"));
            Assert.That(result.Value.Data.DisplayName, Is.EqualTo("Test API"));
        }
    }

    [Test]
    public async Task Api_WhenUpdated_ReflectsNewPath()
    {
        const string apiId = "api-update-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApis()
            .CreateOrUpdateAsync(WaitUntil.Completed, apiId, MinimalApiContent("original-path"))).Value;

        var updated = await created.UpdateAsync(ETag.All, new ApiPatch
        {
            Path = "updated-path",
            DisplayName = "Updated API"
        });

        Assert.That(updated.Value.Data.Path, Is.EqualTo("updated-path"));
    }

    [Test]
    public async Task Api_WhenUpdatedWithMatchingETag_Succeeds()
    {
        const string apiId = "api-update-etag-match";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApis()
            .CreateOrUpdateAsync(WaitUntil.Completed, apiId, MinimalApiContent("etag-path"))).Value;

        var etagResponse = await created.GetEntityTagAsync();
        var eTag = etagResponse.GetRawResponse().Headers.ETag.GetValueOrDefault();
        var updated = await created.UpdateAsync(eTag, new ApiPatch { DisplayName = "Etag Updated" });

        Assert.That(updated.Value.Data.DisplayName, Is.EqualTo("Etag Updated"));
    }

    [Test]
    public async Task Api_WhenUpdatedWithWildcardETag_Succeeds()
    {
        const string apiId = "api-update-wildcard-etag";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApis()
            .CreateOrUpdateAsync(WaitUntil.Completed, apiId, MinimalApiContent("wildcard-path"))).Value;

        var updated = await created.UpdateAsync(ETag.All, new ApiPatch { DisplayName = "Wildcard Updated" });

        Assert.That(updated.Value.Data.DisplayName, Is.EqualTo("Wildcard Updated"));
    }

    [Test]
    public async Task Api_WhenUpdatedWithWrongETag_Throws409()
    {
        const string apiId = "api-update-wrong-etag";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApis()
            .CreateOrUpdateAsync(WaitUntil.Completed, apiId, MinimalApiContent("wrong-etag-path"))).Value;

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await created.UpdateAsync(new ETag("\"wrong-etag-value\""), new ApiPatch { DisplayName = "Should Fail" }));

        Assert.That(ex!.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Api_WhenUpdatedAndNotFound_Throws404()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var fakeApi = armClient.GetApiResource(
            ApiResource.CreateResourceIdentifier(
                SubscriptionId.ToString(), ResourceGroupName, ServiceName, "nonexistent-api"));

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await fakeApi.UpdateAsync(ETag.All, new ApiPatch { DisplayName = "Ghost" }));
    }

    [Test]
    public async Task Api_WhenUpdatedAndParentServiceNotFound_Throws404()
    {
        var armClient = CreateArmClient();

        var fakeApi = armClient.GetApiResource(
            ApiResource.CreateResourceIdentifier(
                SubscriptionId.ToString(), ResourceGroupName, "nonexistent-service", "any-api"));

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await fakeApi.UpdateAsync(ETag.All, new ApiPatch { DisplayName = "Ghost" }));
    }

    [Test]
    public async Task Api_WhenRetrieved_ReturnsCorrectApi()
    {
        const string apiId = "api-get-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApis()
            .CreateOrUpdateAsync(WaitUntil.Completed, apiId, MinimalApiContent("get-path"));

        var result = await service.GetApiAsync(apiId);

        Assert.That(result.Value.Data.Name, Is.EqualTo(apiId));
    }

    [Test]
    public async Task Api_WhenNotFound_GetThrows404()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiAsync("nonexistent-api"));
    }

    [Test]
    public async Task Api_WhenParentServiceNotFound_CreateThrows404()
    {
        var armClient = CreateArmClient();

        var fakeService = armClient.GetApiManagementServiceResource(
            ApiManagementServiceResource.CreateResourceIdentifier(
                SubscriptionId.ToString(), ResourceGroupName, "nonexistent-service"));

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await fakeService.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, "any-api", MinimalApiContent()));
    }

    [Test]
    public async Task Api_List_ContainsCreatedApis()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, "api-list-1", MinimalApiContent("list-path-1"));
        await service.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, "api-list-2", MinimalApiContent("list-path-2"));

        var apis = service.GetApis().GetAll().Select(a => a.Data.Name).ToList();

        Assert.That(apis, Does.Contain("api-list-1"));
        Assert.That(apis, Does.Contain("api-list-2"));
    }
}
